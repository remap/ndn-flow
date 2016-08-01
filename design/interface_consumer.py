# IoT Framework interface

# Library: Consumers -> SequentialConsumer (Subscriber), maybe other types of consumers as well, timestamp/exclusion consumer, and maybe segment fetching consumer; consider Consumer/Producer API's RDR as well?

class SequentialConsumer(object):
    
    def __init__(self, face, keyChain, prefix, onData
      startingSeq = -1, interestLifetimeMilliseconds = 4000, numberOfRetries = 3):
        """
        Initialize a SequentialConsumer instance using given Face, KeyChain, CertificateName, and expresses interests under Prefix namespace.
        Upon starting, SequentialConsumer sends out interest with name "prefix/startingSeq" if StartingSeq is not -1; 
        otherwise it sends the first interest with name "prefix" and RightMostChild, and fill the startingSeq with the sequence number of the first received data

        SequentialConsumer maintains an interest pipeline, whose window of sequence numbers slides by 1 for each piece of data received. 
        Each interest is re-expressed a number of times before this Consumer gives up.

        Whenever data is verified, onData(data) is called. This consumer does not provide reliable delivery (onData may be called in any order, and there could be missing sequence numbers) of data
        Receiving unverified data will still cause the pipeline to slide, but will not call onData callback

        How to use: 
        Create an instance of this class and provide callbacks

        :param Face face: The face to issue interest and receive data
        :param KeyChain keyChain: The keyChain to verify received data
        :param Name prefix: The prefix to which sequence number is attached to construct the interest name
        :param onData: This calls onData(data) once a piece of data is received and verified
        :type onData: function object

        :return: None
        """
        return

    def start(self, resetSeq = -2):
        """
        Start consumer by sending out interest with name "prefix/startingSeq", if startingSeq is -1, send out the first interest with "prefix" and rightMostChild
        If resetSeq is set (not -2), its value overrides startingSeq. Setting resetSeq -1 triggers sending interest with "prefix" and rightMostChild 
        
        :return: None
        """
        return

    def stop(self):
        """
        Stop consumer
        
        :return: None
        """
        return

    def resume(self, resetSeq = -2):
        """
        Resumes consumer if it's paused. Starting from the sequence number and the state of pipeline when paused

        :return: None
        """
        return

    def pause(self):
        """
        Pauses consumer. Keeps the current sequence number and the state of pipeline

        :return: None
        """
        return