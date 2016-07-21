# -*- Mode:python; c-file-style:"gnu"; indent-tabs-mode:nil -*- */
#
# Copyright (C) 2014-2015 Regents of the University of California.
# Author: Jeff Thompson <jefft0@remap.ucla.edu>
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU Lesser General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU Lesser General Public License for more details.
#
# You should have received a copy of the GNU Lesser General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.
# A copy of the GNU Lesser General Public License is in the file COPYING.

# This is based on PyNDN's example of repo_ng insertion and basic consumer

import sys

import time
from pyndn import Name
from pyndn import Face
from pyndn import Interest
from pyndn import Exclude
from pyndn.security import KeyChain

from pyndn.util.memory_content_cache import MemoryContentCache
from pyndn.encoding import ProtobufTlv

# repo-ng protobuf imports
# these imports are produced by:
# protoc --python_out=. repo-command-parameter.proto
# protoc --python_out=. repo-command-response.proto
import repo_command_parameter_pb2
import repo_command_response_pb2

# Arbitrary interest lifetime matters for outstanding interest approach, this value is set to be intentionally larger than publishing interval, so that when data is received, there's likely a PIT entry.
defaultInterestLifetime = 3000

def dump(*list):
    result = ""
    for element in list:
        result += (element if type(element) is str else repr(element)) + " "
    print(result)

class Counter(object):
    def __init__(self, face, repoDataPrefix):
        self._callbackCount = 0
        self._lastTimestamp = ""
        self._face = face
        self._repoDataPrefix = repoDataPrefix

    def onData(self, interest, data):
        self._callbackCount += 1
        dump(data.getContent().toRawStr())
        
        self._lastTimestamp = data.getName().get(-1)       
        exclude = Exclude()
        exclude.appendAny()
        exclude.appendComponent(self._lastTimestamp)
        # First interest should ask for rightmost, to get rid of repo data; while all other interests can ask for leftmost, so that other consumers have less chance of causing data to be excluded
        interest.setChildSelector(0)
        interest.setExclude(exclude)
        interest.setInterestLifetimeMilliseconds(defaultInterestLifetime)
        self._face.expressInterest(interest, self.onData, self.onTimeout)
        
        # Try to insert data into repo
        # Convert data name to repo data name, if they are not the same
        repoDataName = Name(self._repoDataPrefix).append(data.getName().get(-1))
        #self._memCache.add()
        requestInsert(self._face, Name("/home/repo"), repoDataName, None, None)
        
    def onTimeout(self, interest):
        self._callbackCount += 1
        dump("Time out for interest", interest.getName().toUri() + ' ; exclude: ' + interest.getExclude().toUri())
        self._face.expressInterest(interest, self.onData, self.onTimeout)

def requestInsert(face, repoCommandPrefix, fetchName, onInsertStarted, onFailed,
      startBlockId = None, endBlockId = None):
    """
    Send a command interest for the repo to fetch the given fetchName and insert
    it in the repo.
    Since this calls expressInterest, your application must call face.processEvents.
    :param Face face: The Face used to call makeCommandInterest and expressInterest.
    :param Name repoCommandPrefix: The repo command prefix.
    :param Name fetchName: The name to fetch. If startBlockId and endBlockId are
      supplied, then the repo will request multiple segments by appending the
      range of block IDs (segment numbers).
    :param onInsertStarted: When the request insert command successfully returns,
      this calls onInsertStarted().
    :type onInsertStarted: function object
    :param onFailed: If the command fails for any reason, this prints an error
      and calls onFailed().
    :type onFailed: function object
    :param int startBlockId: (optional) The starting block ID (segment number)
      to fetch.
    :param int endBlockId: (optional) The end block ID (segment number)
      to fetch.
    """
    parameter = repo_command_parameter_pb2.RepoCommandParameterMessage()
    # Add the Name.
    for i in range(fetchName.size()):
        parameter.repo_command_parameter.name.component.append(
          fetchName[i].getValue().toBytes())
    # Add startBlockId and endBlockId if supplied.
    if startBlockId != None:
        parameter.repo_command_parameter.start_block_id = startBlockId
    if endBlockId != None:
        parameter.repo_command_parameter.end_block_id = endBlockId

    # Create the command interest.
    interest = Interest(Name(repoCommandPrefix).append("insert")
      .append(Name.Component(ProtobufTlv.encode(parameter))))
    face.makeCommandInterest(interest)
    face.expressInterest(interest, onRepoData, onRepoTimeout)

# Send the command interest and get the response or timeout.
def onRepoData(interest, data):
    # repo_command_response_pb2 was produced by protoc.
    response = repo_command_response_pb2.RepoCommandResponseMessage()
    try:
        ProtobufTlv.decode(response, data.content)
    except:
        dump("Cannot decode the repo command response")
        onFailed()

    if response.repo_command_response.status_code == 100:
        #onInsertStarted()
        pass
    else:
        dump("Got repo command error code", response.repo_command_response.status_code)
        #onFailed()
        pass

def onRepoTimeout(interest):
    dump("Insert repo command timeout")
    onFailed()
    
def onRegisterFailed(prefix):
    print('Could not register prefix: ' + prefix.getName().toUri())

def onDataNotFound(prefix, interest, face, interestFilterId, interestFilter):
    print('Data not found for: ' + interest.getName().toUri())

def main():
    # The default Face will connect using a Unix socket, or to "localhost".
    face = Face()
    keyChain = KeyChain()
    face.setCommandSigningInfo(keyChain, keyChain.getDefaultCertificateName())

    dataPrefix = "/home/test1/data"
    repoDataPrefix = "/home/test1/data"
    # Set up repo-ng, register prefix for repo-ng's fetch prefix
    # Per configuration file in /usr/local/etc/ndn/repo-ng.conf
    # memCache is not used for now; repo is hoping that the piece of data in question is still being held at nfd
    #memCache = MemoryContentCache(face, 100000)
    #memCache.registerPrefix(Name(repoDataPrefix), onRegisterFailed, onDataNotFound)

    counter = Counter(face, repoDataPrefix)

    interest = Interest(Name(dataPrefix))
    interest.setChildSelector(1)
    interest.setInterestLifetimeMilliseconds(defaultInterestLifetime)
    face.expressInterest(interest, counter.onData, counter.onTimeout)
    
    while True:
        face.processEvents()
        # We need to sleep for a few milliseconds so we don't use 100% of the CPU.
        time.sleep(1)
    face.shutdown()

main()
