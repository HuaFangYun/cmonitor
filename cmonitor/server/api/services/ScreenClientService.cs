﻿using cmonitor.server.client.reports.screen;
using cmonitor.server.service;
using cmonitor.server.service.messengers.screen;
using cmonitor.server.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
{
    public sealed class ScreenClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly Config config;
        public ScreenClientService(MessengerSender messengerSender, SignCaching signCaching, Config config)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.config = config;
        }
        public bool Full(ClientServiceParamsInfo param)
        {
            ScreenReportInfo report = param.Content.DeJson<ScreenReportInfo>();
            byte[] bytes = new byte[] {(byte)report.Type };
            for (int i = 0; i < report.Names.Length; i++)
            {
                bool connectionRes = signCaching.Get(report.Names[i], out SignCacheInfo cache) && cache.Connected;
                if (connectionRes == false) continue;
                bool reportRes = cache.GetScreen(config.ScreenDelay) && Interlocked.CompareExchange(ref cache.ScreenFlag, 0, 1) == 1;

                if (connectionRes && reportRes)
                {
                    cache.UpdateScreen();
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.CaptureFull,
                        Timeout = 1000,
                         Payload= bytes
                    }).ContinueWith((result) =>
                    {
                        Interlocked.Exchange(ref cache.ScreenFlag, 1);
                    });
                }
            }

            return true;
        }
        public bool Clip(ClientServiceParamsInfo param)
        {
            ScreenClipParamInfo screenClipParamInfo = param.Content.DeJson<ScreenClipParamInfo>();
            if (signCaching.Get(screenClipParamInfo.Name, out SignCacheInfo cache))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ScreenMessengerIds.CaptureClip,
                    Timeout = 1000,
                    Payload = MemoryPackSerializer.Serialize(screenClipParamInfo.Clip)
                });
            }
            return true;
        }

        public bool Region(ClientServiceParamsInfo param)
        {
            string[] names = param.Content.DeJson<string[]>();
            for (int i = 0; i < names.Length; i++)
            {
                bool res = signCaching.Get(names[i], out SignCacheInfo cache)
                    && cache.Connected
                    && cache.GetScreen(config.ScreenDelay)
                    && Interlocked.CompareExchange(ref cache.ScreenFlag, 0, 1) == 1;
                if (res)
                {
                    cache.UpdateScreen();
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.CaptureRegion,
                        Timeout = 1000,
                    }).ContinueWith((result) =>
                    {
                        Interlocked.Exchange(ref cache.ScreenFlag, 1);
                    });
                }
            }

            return true;
        }

    }

    public sealed class ScreenReportInfo
    {
        public string[] Names { get; set; }
        public ScreenReportFullType Type { get; set; }
    }
    public sealed class ScreenClipParamInfo
    {
        public string Name { get; set; }
        public ScreenClipInfo Clip { get; set; }
    }
}
