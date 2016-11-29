// need IotNode for "getSerial" function

var bootstrap = undefined;
var face = undefined;
var myKeyChain = undefined;
var myCertificateName = undefined;
var memoryContentCache = undefined;

function onSetupComplete(defaultCertificateName, keyChain)
{
    console.log("Default certificate name and keyChain set up complete.");
    
    // default publishing instance, not required
    /*
    var dataPrefix = new Name("/home/flow/browser-publisher-1");
    var appName = "flow";

    myKeyChain = keyChain;
    myCertificateName = defaultCertificateName;
    memoryContentCache = new MemoryContentCache(face);
    memoryContentCache.registerPrefix(dataPrefix, function (prefix) {
        console.log("Register failed for prefix: " + prefix.toUri());
    }, function (prefix, interest, face, interestFilterId, filter) {
        console.log("Got interest unable to answer: " + interest.getName().toUri());
    });
    */

    // TODO: extend requestProducerAuthorization to handle command interest sending requests
    //bootstrap.requestProducerAuthorization(dataPrefix, appName, onRequestSuccess, onRequestFailed);

    return;
}

/*
function onRequestSuccess()
{
    console.log("Requested granted by controller.");
    var data = new Data(new Name("/home/flow/browser-publisher-1/test"));
    data.setContent("test!");
    data.getMetaInfo().setFreshnessPeriod(400000);
    myKeyChain.sign(data, myCertificateName, function (data) {
        memoryContentCache.add(data);
    }, function (error) {
        console.log(error);
    });
    return;
}

function onRequestFailed(msg)
{
    console.log("data production not authorized by controller : " + msg);
    // For this test, we produce anyway
    var data = new Data(new Name("/home/flow/browser-publisher-1/test"));
    data.setContent("test!");
    data.getMetaInfo().setFreshnessPeriod(400000);
    myKeyChain.sign(data, myCertificateName, function (data) {
        memoryContentCache.add(data);
    }, function (error) {
        console.log(error);
    });
    return;
}
*/

function onSetupFailed(msg)
{
    console.log("Setup failed " + msg);
}