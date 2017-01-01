class AppConsumer():
    """
    The interface for common application consumers (e.g. sequence
    number based and timestamp based)

    :param face: the face to consume data with
    :type face: Face
    :param keyChain: the keyChain to verify received data with
    :type keyChain: KeyChain
    :param doVerify: flag for whether the consumer should skip verification
    :type doVerify: bool
    """
    def __init__(self, face, keyChain, doVerify):
        self._face = face
        self._keyChain = keyChain
        self._doVerify = doVerify

        return

    def consume(self, name, onData, onVerifyFailed, onTimeout):
        """
        Consume one piece of data, or consume continuously, depending on
        child class's implementation

        :param name: name / prefix to consume data under
        :type name: Name
        :param onData: onData(data) gets called after received data's onVerifyFailed
        :type onData: function object
        :param onVerifyFailed: onVerifyFailed(data) gets called if received data 
          cannot be verified
        :type onVerifyFailed: function object
        :param onTimeout: onTimeout(interest) gets called if a consumer interest times out
        :type onTimeout: function object
        """
        return
