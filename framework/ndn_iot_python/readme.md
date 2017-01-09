NDN-IoT in Python
====================

This page describes how to compile and install the Python NDN-IoT framework, and examples to use it in your code.

### Compile and install
```
cd ndn_iot_python
python setup.py install --user
```

### Dependency
* Python 2.7 (Python 3 coming soon)

* [PyNDN2](https://github.com/named-data/PyNDN2/blob/master/INSTALL.md) (>= v2.4b1, for OnDataValidationFailed support)

* (may need to regenerate protobuf files, for the specific version of protobuf installed)

### Examples
* [Bootstrap - basic consumer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_python/examples/test_consuming.py)

* [Bootstrap - basic producer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_python/examples/test_producing.py)

* [Discovery](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_python/examples/test_discovery.py)

* Consumer - timestamp consumer (coming soon)

* Consumer - sequence number consumer (coming soon)

### Using in your code
If installed, import library; if not, set PYTHONPATH