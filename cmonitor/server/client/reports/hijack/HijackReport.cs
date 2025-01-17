﻿using cmonitor.hijack;
using common.libs;
using MemoryPack;

namespace cmonitor.server.client.reports.hijack
{
    public sealed class HijackReport : IReport
    {
        public string Name => "Hijack";

        private readonly HijackEventHandler hijackEventHandler;
        private readonly HijackConfig hijackConfig;
        private readonly ClientConfig clientConfig;
        private readonly HijackController hijackController;

        private ulong[] array = new ulong[3];
        private ulong[] lastArray = new ulong[3];
        private long ticks = DateTime.UtcNow.Ticks;

        public HijackReport(HijackEventHandler hijackEventHandler, HijackController hijackController, HijackConfig hijackConfig, ClientConfig clientConfig, Config config)
        {
            this.hijackEventHandler = hijackEventHandler;
            this.hijackController = hijackController;
            this.hijackConfig = hijackConfig;
            this.clientConfig = clientConfig;
            if (OperatingSystem.IsWindows() && config.IsCLient)
            {
                try
                {
                    hijackConfig.DeniedProcesss = clientConfig.HijackConfig.DeniedProcesss;
                    hijackConfig.AllowProcesss = clientConfig.HijackConfig.AllowProcesss;
                    hijackConfig.DeniedDomains = clientConfig.HijackConfig.DeniedDomains;
                    hijackConfig.AllowDomains = clientConfig.HijackConfig.AllowDomains;
                    hijackConfig.DeniedIPs = clientConfig.HijackConfig.DeniedIPs;
                    hijackConfig.AllowIPs = clientConfig.HijackConfig.AllowIPs;

                    hijackController.Start();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }

        public void Update(SetRuleInfo info)
        {
            hijackConfig.AllowDomains = info.AllowDomains;
            hijackConfig.DeniedDomains = info.DeniedDomains;
            hijackConfig.AllowProcesss = info.AllowProcesss;
            hijackConfig.DeniedProcesss = info.DeniedProcesss;
            hijackConfig.AllowIPs = info.AllowIPs;
            hijackConfig.DeniedIPs = info.DeniedIPs;

            clientConfig.HijackConfig = hijackConfig;

            hijackController.SetRules();
        }

        public object GetReports(ReportType reportType)
        {
            array[0] = hijackEventHandler.UdpSend + hijackEventHandler.TcpSend;
            array[1] = hijackEventHandler.TcpReceive + hijackEventHandler.UdpReceive;
            ulong count = (ulong)(hijackConfig.AllowIPs.Length + hijackConfig.DeniedIPs.Length + hijackConfig.AllowDomains.Length + hijackConfig.DeniedDomains.Length + hijackConfig.AllowProcesss.Length + hijackConfig.DeniedProcesss.Length);
            array[2] = count;

            long _ticks = DateTime.UtcNow.Ticks;
            if (((_ticks - ticks) / TimeSpan.TicksPerMillisecond >= 300 && array.SequenceEqual(lastArray) == false) || reportType == ReportType.Full)
            {
                ticks = _ticks;
                lastArray[0] = array[0];
                lastArray[1] = array[1];
                lastArray[2] = array[2];
                return array;
            }
            return null;
        }
    }

    [MemoryPackable]
    public sealed partial class SetRuleInfo
    {
        /// <summary>
        /// 进程白名单
        /// </summary>
        public string[] AllowProcesss { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 进程黑名单
        /// </summary>
        public string[] DeniedProcesss { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 域名白名单
        /// </summary>
        public string[] AllowDomains { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 域名黑名单
        /// </summary>
        public string[] DeniedDomains { get; set; } = Array.Empty<string>();

        /// <summary>
        /// ip白名单
        /// </summary>
        public string[] AllowIPs { get; set; } = Array.Empty<string>();
        /// <summary>
        /// ip黑名单
        /// </summary>
        public string[] DeniedIPs { get; set; } = Array.Empty<string>();
    }
}
