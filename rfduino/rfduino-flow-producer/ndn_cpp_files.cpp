// Include needed .cpp files from ndn-cpp.

#include "ndn_cpp_root.h"

// Note: ndn-cpp-config.h should have the following:
// #define NDN_CPP_WITH_ARDUINOLIBS 1

#include NDN_CPP_ROOT(pp/src/lite/data-lite.cpp)
#include NDN_CPP_ROOT(pp/src/lite/interest-lite.cpp)
#include NDN_CPP_ROOT(pp/src/lite/key-locator-lite.cpp)
#include NDN_CPP_ROOT(pp/src/lite/meta-info-lite.cpp)
#include NDN_CPP_ROOT(pp/src/lite/name-lite.cpp)
#include NDN_CPP_ROOT(pp/src/lite/signature-lite.cpp)
#include NDN_CPP_ROOT(pp/src/lite/util/blob-lite.cpp)
#include NDN_CPP_ROOT(pp/src/lite/util/crypto-lite.cpp)
#include NDN_CPP_ROOT(pp/src/lite/encoding/tlv-0_1_1-wire-format-lite.cpp)

#include NDN_CPP_ROOT(pp/contrib/arduinolibs/libraries/Crypto/Crypto.cpp)
#include NDN_CPP_ROOT(pp/contrib/arduinolibs/libraries/Crypto/Hash.cpp)
#include NDN_CPP_ROOT(pp/contrib/arduinolibs/libraries/Crypto/SHA256.cpp)
#include NDN_CPP_ROOT(pp/contrib/arduinolibs/libraries/Crypto/SHA512.cpp)
#include NDN_CPP_ROOT(pp/contrib/arduinolibs/libraries/Crypto/ChaCha.cpp)
#include NDN_CPP_ROOT(pp/contrib/arduinolibs/libraries/Crypto/RNG.cpp)
#include NDN_CPP_ROOT(pp/contrib/arduinolibs/libraries/Crypto/P521.cpp)
#include NDN_CPP_ROOT(pp/contrib/arduinolibs/libraries/Crypto/BigNumberUtil.cpp)

