rfduino-flow-producer for the RFduino
=====================================

These are instructions to build rfduino-flow-producer, the ndn-flow producer application for RFduino.

In the following, <NDN-FLOW root> is the root of the ndn-flow distribution and
<NDN-CPP root> is the root of the NDN-CPP distribution.

Download https://www.schneier.com/sccd/RSAREF20.ZIP . Enter:

    cd <NDN-CPP root>/contrib
    mkdir rsaref
    cd <NDN-CPP root>/contrib/rsaref
    unzip -LL RSAREF20.ZIP

(Change RSAREF20.ZIP to the path where you downloaded the file.) The "-LL" is
important because it changes the file names to lower case. Now do the following
to update the RSAREF code:

    sed -i '' 's/MAX_RSA_MODULUS_BITS 1024/MAX_RSA_MODULUS_BITS 2048/g' source/rsaref.h

To compile NDN-CPP, enter:

    cd <NDN-CPP root>
    ./configure

Arduino does not have memory.h, so edit <NDN-CPP root>/include/ndn-cpp/ndn-cpp-config.h
and change:

    #define NDN_CPP_HAVE_MEMORY_H 1

to

    #define NDN_CPP_HAVE_MEMORY_H 0

Download and uncompress the Arduino IDE from http://www.arduino.cc/en/Main/Software .
In the following <ARDUINO> is the Arduino directory.
The following is a simple way to get the NDN-CPP and RSAREF include directories in the
Arduino build path. Change to the directory containing RFduinoBLE.h, (for example,
/home/myuser/Library/Arduino15/packages/RFduino/hardware/RFduino/2.3.2/libraries/RFduinoBLE)
and enter:

    ln -s <NDN-CPP root>/include/ndn-cpp
    ln -s <NDN-CPP root>/contrib/rsaref

Start the Arduino IDE.

Click the menu File >> Open and open 
<NDN-FLOW>/rfduino/rfduino-flow-producer/rfduino-flow-producer.ino .
In the tab ndn_cpp_root.h, change "/please/fix/NDN_CPP_ROOT/in/ndn_cpp_root.h/ndn-c" to
the path up to the NDN-CPP root. Keep the "/ndn-c" at the end. For example, if
<NDN-CPP> is "/home/myuser/ndn-cpp" then use "/home/myuser/ndn-c" .

To compile, click the checkbox icon.
