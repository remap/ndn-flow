var Producer = function Producer(face, keyChain, certificateName, memoryContentCache) {
    this.keyChain = keyChain;
    this.certificateName = certificateName;
    this.face = face;
    if (memoryContentCache === undefined) {
        this.memoryContentCache = new MemoryContentCache(face);
    } else {
        this.memoryContentCache = memoryContentCache
    }
}

Producer.prototype.registerPrefix = function(prefix) {
    this.memoryContentCache.registerPrefix(prefix, function () {
        console.log("Prefix registration failed: " + prefix.toUri());
    }, function (interest) {
        console.log("Data not found for: " + interest.getName().toUri());
    });
}

// TODO: overloads for .produce call
Producer.prototype.produce = function(name, content) {
    var data = new Data(new Name(name));
    data.setContent(content);
    var self = this;
    this.keyChain.sign(data, function () {
        self.memoryContentCache.add(data);
    }, function (exception) {
        console.log("error signing data: " + exception);
    });
}