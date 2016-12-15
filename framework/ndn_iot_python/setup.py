# -*- Mode: python; py-indent-offset: 4; indent-tabs-mode: nil; coding: utf-8; -*-

# This uses the template https://github.com/pypa/sampleproject/blob/master/setup.py
# and from Alex Afanasyev's file at https://github.com/cawka/PyNDN2/blob/master/setup.py

# To build/upload the package, do the following as described in
# https://python-packaging-user-guide.readthedocs.org/en/latest/distributing.html
# sudo python setup.py sdist
# sudo python setup.py bdist_wheel --universal
# sudo python setup.py sdist bdist_wheel upload

from setuptools import setup, find_packages  # Always prefer setuptools over distutils
import sys

requirements = []
# TODO: test installation of this without pyndn present
if sys.version_info[0] == 2:
    requirements.append('trollius')
    requirements.append('protobuf')
elif sys.version_info[0] == 3:
    requirements.append('protobuf-py3')
    if sys.version_info[1] < 3:
        requirements.append('trollius')
    elif sys.version_info[1] < 4:
        requirements.append('asyncio')

setup(
    name='ndn_iot_python',

    version='0.1',

    description='NDNoT based on PyNDN2',

    url='https://github.com/remap/ndn-flow',

    maintainer='Zhehao Wang',
    maintainer_email='zhehao@cs.ucla.edu',

    license='LGPLv3',

    # See https://pypi.python.org/pypi?%3Aaction=list_classifiers
    classifiers=[
        'Development Status :: 4 - Beta',

        'Intended Audience :: Developers',
        'Topic :: Software Development :: Libraries',

        'License :: OSI Approved :: GNU Lesser General Public License v3 or later (LGPLv3+)',

        'Programming Language :: Python :: 2',
        'Programming Language :: Python :: 2.6',
        'Programming Language :: Python :: 2.7',
    ],

    keywords='ndn_iot_python',

    packages=find_packages('python'),
    package_dir = {'':'python'},

    install_requires=requirements
)
