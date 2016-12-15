namespace ndn_iot.util {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    using net.named_data.jndn;

    // internal functions
    class Util {
        static public string dataContentToString(Data data) {
            var content = data.getContent().buf();
            var contentString = "";
            for (int i = content.position(); i < content.limit(); ++i)
                contentString += (char)content.get(i);
            return contentString;
        }
    }
    
}