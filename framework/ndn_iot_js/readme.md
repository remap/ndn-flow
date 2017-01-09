NDN-IoT in JavaScript
====================

This page describes how to install the JavaScript NDN-IoT framework, and examples to use it in your code.

### Generate combined script
```
cd ndn_iot_js
./waf configure
./waf
```

### Examples
* [Bootstrap - basic consumer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_js/examples/test-consuming.html)

* [Bootstrap - basic producer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_js/examples/test-producing.html)

* [Discovery](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_js/examples/test-discovery.html)

* Consumer - timestamp consumer (coming soon)

* Consumer - sequence number consumer (coming soon)

### Additional content

add-device.html, an ndn_pi client for adding a mobile device to the home network, is also included [here](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_js/js/add-device/add-device.html).

To use the script on a mobile device, be sure to use latest Firefox for Android, as Chrome would complain about "only secure origins are allowed", and Android browser would complain about "promise not supported" for KeyChain.createIdentityAndCertificate function.

### Using in your code
Include the combined js and ndn-js.js