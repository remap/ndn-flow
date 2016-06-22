rfduino-flow-producer for the RFduino
=====================================

These are instructions to build rfduino-flow-producer, the ndn-flow producer application for RFduino.

In the following, <NDN-FLOW root> is the root of the ndn-flow distribution and
<NDN-CPP root> is the root of the NDN-CPP distribution. Enter:

    cd <NDN-CPP root>
    ./configure

Arduino does not have memory.h, so edit <NDN-CPP root>/include/ndn-cpp/ndn-cpp-config.h
and change:

    #define NDN_CPP_HAVE_MEMORY_H 1

to

    #define NDN_CPP_HAVE_MEMORY_H 0

Download and uncompress the Arduino IDE from http://www.arduino.cc/en/Main/Software .
In the following <ARDUINO> is the Arduino directory.
The following is a simple way to get the NDN-CPP public include directory in the
Arduino build path. Change to the directory <ARDUINO>/hardware/tools/avr/avr/include
and enter:

    ln -s <NDN-CPP root>/include/ndn-cpp

Start the Arduino IDE.

Click the menu File >> Open and from the NDN-CPP root select 
<NDN-FLOW>/rfduino/rfduino-flow-producer/rfduino-flow-producer.ino .
In the tab ndn_cpp_root.h, change "/please/fix/NDN_CPP_ROOT/in/ndn_cpp_root.h/ndn-c" to
the path up to the NDN-CPP root. Keep the "/ndn-c" at the end. For example, if
<NDN-CPP> is "/home/myuser/ndn-cpp" then use "/home/myuser/ndn-c" .

To compile, click the checkbox icon.
