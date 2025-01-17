﻿using cmonitor.server.service;
using cmonitor.server.service.messengers.sign;
using cmonitor.server.service.messengers.volume;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
{
    public sealed class VolumeClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public VolumeClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }


        public async Task<bool> Update(ClientServiceParamsInfo param)
        {
            VolumeInfo info = param.Content.DeJson<VolumeInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Value);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)VolumeMessengerIds.Update,
                        Payload = bytes
                    });
                }
            }

            return true;
        }

        public async Task<bool> Mute(ClientServiceParamsInfo param)
        {
            VolumeMuteInfo info = param.Content.DeJson<VolumeMuteInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Value);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)VolumeMessengerIds.Mute,
                        Payload = bytes
                    });
                }
            }

            return true;
        }
    }

    public sealed class VolumeInfo
    {
        public string[] Names { get; set; }
        public float Value { get; set; }
    }
    public sealed class VolumeMuteInfo
    {
        public string[] Names { get; set; }
        public bool Value { get; set; }
    }
}
