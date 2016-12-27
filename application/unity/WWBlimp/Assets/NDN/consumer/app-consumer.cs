// conform CS namespace definition with that of cpp
namespace ndn_iot.consumer {
    using System;

    using net.named_data.jndn;
    using net.named_data.jndn.security;
    
    public interface AppConsumer {
        void consume(Name name, OnVerified onVerified, OnDataValidationFailed onVerifyFailed, OnTimeout onTimeout);
    }
}