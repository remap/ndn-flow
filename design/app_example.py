#!/usr/bin/python

class IotApplication():
    def __init__(self):
        # Initialize face
        self.loop = asyncio.get_event_loop()
        self.face = ThreadsafeFace(self.loop)

    def start(self):
        # Creates an instance of Bootstrap class, which bootstraps keyChain, default identity and certificate name (for publishing), and policy manager (for consuming)
        # This application does both publishing and consuming, so we call setupKeyChain and startTrustSchemaUpdate
        bootstrap = Bootstrap(self.face)
        bootstrap.setupKeyChain("app.conf", onSetupComplete = self.onSetupComplete, onSetupFailed = self.onSetupFailed)
        bootstrap.startTrustSchemaUpdate(self, namespace, onUpdateSuccess = self.onUpdateSuccess, onUpdateFailed = self.onUpdateFailed)

        self.loop.run_forever()

    def onSetupComplete(self, defaultIdentity, keyChain):
        # KeyChain setup successul, this application can now publish, and run discovery (which requires publishing functions to be set up)
        producer = AppProducerMemoryContentCache(appProducePrefix)
        producer.produce(someSuffix, content)
        discovery = Discovery()
        discovery.start()

    def onSetupFailed(self, msg):
        return

    def onUpdateSuccess(self, initialUpdate):
        # Initial trust schema is received and verified 
        # (to verify initial trust schema, the bootstrap instance loads a default trust schema with trust anchor cert, written by the add_device process)
        # all later contents can be verified so we start consuming
        if initialUpdate:
            consumer = SequenceNumberConsumer(appConsumePrefix)
            consumer.consume(someSuffix, onComplete, onFailed)

    def onUpdateFailed(self, msg):
        return

if __name__ == '__main__':
    try:
        import psutil as ps
    except Exception as e:
        print str(e)
    myIotApplication = IotApplication()

    loop.run_forever()
