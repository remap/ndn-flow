function expressInterestWithExclusion(prefix, exclusion, leftmost) {
  var interest = new Interest(new Name(prefix));
  interest.setInterestLifetimeMilliseconds(4000);
  interest.setMustBeFresh(true);
  if (exclusion === undefined) {

  } else {
    interest.setExclude(exclusion);
  }
  if (leftmost === undefined || leftmost === true) {
    interest.setChildSelector(0);
  } else {
    interest.setChildSelector(1);
  }
  console.log("about to express interest: " + interest.getName().toUri());
  if (exclusion !== undefined) {
    console.log("with exclude: " + interest.getExclude().toUri());
  }
  face.expressInterest(interest, onData, onTimeout);
}

function onData(interest, data) {
  console.log("got data: " + data.getName().toUri());
  insertToTree(data);

  var interestName = interest.getName();
  var dataName = data.getName();
  // data is longer than interest, we probably should ask with exclusion
  if (dataName.size() > interestName.size()) {
    // if data name is longer than interest name by only one component
    if (dataName.size() - interestName.size() == 1) {
      // we only express interest interest with exclusion if the longer-than-interest-name-size-by-one data name has
      // sequence number, or
      // version number, or
      // segment number, or
      // pure number, or
      // sync digest
      // as the last element
      var lastComponent = dataName.get(-1);
      try {
        // version 
        var version = lastComponent.toVersion();
        console.log("finished probing this branch (data ending with version): " + interestName.toUri());
        return;
      } catch (exception) {

      }
      try {
        // segment
        var segment = lastComponent.toSegment();
        console.log("finished probing this branch (data ending with segment): " + interestName.toUri());
        return;
      } catch (exception) {

      } 
      try {
        // pure number
        var numbers = lastComponent.toEscapedString();
        var containsNonNumbers = false;
        for (var i = 0; i < numbers.length; i ++) {
          if (numbers.charCodeAt(i) >= 48 && numbers.charCodeAt(i) <= 58) {
            continue;
          } else {
            containsNonNumbers = true;
          }
        }
        if (!containsNonNumbers) {
          console.log("finished probing this branch (data ending with only numbers): " + interestName.toUri());
          return;
        }
      } catch (exception) {

      } 
    }

    var component = dataName.get(interestName.size());
    
    // ask for the next piece of data excluding the last component
    var exclusion = new Exclude();
    exclusion.appendAny();
    exclusion.appendComponent(component);
    expressInterestWithExclusion(interestName, exclusion, true);
    // ask for the first piece of data in a subnamespace, 
    // this data will be able to satisfy the interest, in that case, the next exclusion interest should fetch later data in that branch
    var newPrefix = new Name(dataName.getPrefix(interestName.size() + 1));
    console.log("new prefix: " + newPrefix.toUri());
    expressInterestWithExclusion(newPrefix, undefined, true);
  } else {
    // data is no longer interest, we are done probing this branch
    console.log("finished probing this branch (data length = interest length): " + interestName.toUri());
    return;
  }
  
}

function onTimeout(interest) {
  console.log("interest times out: " + interest.getName().toUri());
}