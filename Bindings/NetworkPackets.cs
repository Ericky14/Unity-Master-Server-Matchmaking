using System;
using System.Collections.Generic;

namespace Bindings {
    public enum ServerPackets {
        SAlert,
        SAskIfClientOrServer,
        SConnectToMatch,
        SMatchConnectionReady,
        SPlayerConnectionReady,
        SSendPlayerData
    }

    public enum ClientPackets {
        CConnectionReady,
        CFindMatch,
        CMatchServerStarted,
        CRequestPlayerData,
        CSendKey
    }
}
