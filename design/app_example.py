#!/usr/bin/python

"""
This is an example of calls an application would make to interact with the interface of the IoT framework.
Covered functions include:
 - Bootstrapping the application (not the device, which is covered by framework/ndn_pi), including its keyChain, defaultIdentity and policyManager.
 - Running a producer and consumer after boostrapping, and running discovery
"""

class IotApplication():
    def __init__(self):
        # Initialize face
        self.face = ThreadsafeFace(self.loop)

    def start(self):
        # Creates an instance of Bootstrap class, which bootstraps keyChain, default identity and certificate name (for publishing), and policy manager (for consuming)
        # This application does both publishing and consuming, so we call setupKeyChain and startTrustSchemaUpdate
        bootstrap = Bootstrap(self.face)
        bootstrap.setupDefaultIdentityAndRoot("app.conf", onSetupComplete, onSetupFailed)

    def onSetupComplete(defaultIdentity, keyChain):
        # KeyChain setup successul
        # Requests production authorization (trust schema update and distribution by controller)
        bootstrap.requestProducerAuthorization(applicationProducingNamespace, appName, deviceIdentity, onRequestSuccess, onRequestFailed)
        # Requests trust schema so that we can authenticate consumed messages
        bootstrap.startTrustSchemaUpdate(Name(applicationTrustSchemaNamespace), self.onUpdateSuccess, self.onUpdateFailed)    

    def onRequestSuccess(self):
        # Producing permission granted; this application can now publish, and run discovery (which requires publishing functions to be set up)
        producer = AppProducerMemoryContentCache(self.face, self.keyChain, self.defaultCertificateName, applicationProducingNamespace)
        producer.produce(someSuffix, content)
        
        discovery = Discovery(self.face, self.keyChain, self.defaultCertificateName, discoveryNamespace)
        discovery.start()
        return

    def onUpdateSuccess(self, trustSchemaString, isInitial):
        # Initial trust schema is received and verified 
        # (to verify initial trust schema, the bootstrap instance loads a default trust schema with trust anchor cert, written by the add_device process)
        # all later contents can be verified so we start consuming
        if initialUpdate:
            consumer = SequenceNumberConsumer(self.face, self.keyChain, self.defaultCertificateName, applicationConsumingNamespace)
            consumer.consume(someSuffix, onComplete, onFailed)

if __name__ == '__main__':
    loop = asyncio.get_event_loop()
    face = ThreadsafeFace(loop)
    
    myIotApplication = IotApplication(face)
    myIotApplication.start()

    loop.run_forever()
