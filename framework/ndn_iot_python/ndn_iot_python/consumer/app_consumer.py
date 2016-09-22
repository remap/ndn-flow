class AppConsumer():
	def __init__(self, face, keyChain, certificateName, doVerify):
		self._face = face
		self._keyChain = keyChain
		self._certificateName = certificateName
		self._doVerify = doVerify

		return

	def consume(self, name, onData, onVerificationFailed, onTimeout):
		return
