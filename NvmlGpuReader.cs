using System;
using System.Runtime.InteropServices;

namespace Flower_Pomodoro_Timer
{
    /// <summary>
    /// NVIDIA NVML（NVIDIA Management Library）讀取器，透過 P/Invoke 呼叫 nvml.dll。
    ///
    /// 設計動機：
    /// 舊版以 PerformanceCounter 統計 VRAM 需遍歷「每個程序」的顯存用量，
    /// 既慢又脆。NVML 直接讀取 driver 維護的全域顯存計數器，
    /// 一次呼叫（O(1)）即取得 total / used / free，是 nvidia-smi 與
    /// ComfyUI-Crystools 內部採用的同一套官方 API，但跳過了啟動子程序的開銷。
    ///
    /// 限制：僅支援 NVIDIA 顯示卡（nvml.dll 隨 NVIDIA 驅動安裝）。
    /// 非 NVIDIA 機器上 TryCreate() 會回傳 null，呼叫端應自行 fallback 至
    /// PerformanceCounter 路徑。
    /// </summary>
    internal sealed class NvmlGpuReader : IDisposable
    {
        /// <summary>NVML 裝置控制碼（不透明指標），指向第 0 張顯示卡。</summary>
        private IntPtr m_device;
        /// <summary>NVML 是否已成功初始化並取得裝置控制碼。</summary>
        private bool m_initialized;

        /// <summary>NVML 是否可用（已初始化且取得裝置）。</summary>
        public bool Available => m_initialized;

        private NvmlGpuReader() { }

        /// <summary>
        /// 嘗試初始化 NVML 並取得第 0 張顯示卡的控制碼。
        /// 成功回傳可用的讀取器；失敗（非 NVIDIA、驅動過舊、dll 不存在）回傳 null。
        /// </summary>
        public static NvmlGpuReader? TryCreate()
        {
            // 部分舊驅動未將 nvml.dll 放入 System32，先嘗試從 NVSMI 安裝目錄預先載入，
            // 讓後續 DllImport 能正確解析。新驅動則已放入 System32，此步可有可無。
            try
            {
                if (LoadLibrary("nvml.dll") == IntPtr.Zero)
                {
                    LoadLibrary(@"C:\Program Files\NVIDIA Corporation\NVSMI\nvml.dll");
                }
            }
            catch { }

            var reader = new NvmlGpuReader();
            try
            {
                if (nvmlInit_v2() != NVML_SUCCESS)
                {
                    return null;
                }

                if (nvmlDeviceGetHandleByIndex_v2(0, out IntPtr device) != NVML_SUCCESS)
                {
                    try { nvmlShutdown(); } catch { }
                    return null;
                }

                reader.m_device = device;
                reader.m_initialized = true;
                return reader;
            }
            catch
            {
                // DllNotFoundException（無 nvml.dll）或其他例外：視為不支援
                try { nvmlShutdown(); } catch { }
                return null;
            }
        }

        /// <summary>
        /// 讀取第 0 張顯示卡的顯存資訊（位元組）。
        /// </summary>
        /// <param name="total">顯存總量</param>
        /// <param name="used">已用顯存</param>
        /// <param name="free">剩餘顯存</param>
        /// <returns>讀取成功與否</returns>
        public bool TryGetMemory(out ulong total, out ulong used, out ulong free)
        {
            total = used = free = 0;
            if (!m_initialized) return false;
            try
            {
                NvmlMemory mem = default;
                if (nvmlDeviceGetMemoryInfo(m_device, ref mem) != NVML_SUCCESS) return false;
                total = mem.total;
                free = mem.free;
                used = mem.used;
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 讀取第 0 張顯示卡的 GPU 核心使用率（百分比 0~100）。
        /// </summary>
        public bool TryGetGpuUtilization(out uint gpuPercent)
        {
            gpuPercent = 0;
            if (!m_initialized) return false;
            try
            {
                NvmlUtilization util = default;
                if (nvmlDeviceGetUtilizationRates(m_device, ref util) != NVML_SUCCESS) return false;
                gpuPercent = util.gpu;
                return true;
            }
            catch { return false; }
        }

        public void Dispose()
        {
            if (m_initialized)
            {
                try { nvmlShutdown(); } catch { }
                m_initialized = false;
            }
        }

        // ── NVML 結構與函式宣告 ────────────────────────────────────────

        /// <summary>NVML 回傳碼：0 表示成功（NVML_SUCCESS）。</summary>
        private const int NVML_SUCCESS = 0;

        /// <summary>對應 nvmlMemory_t：顯存總量／剩餘／已用（位元組）。欄位順序不可變動。</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NvmlMemory
        {
            public ulong total;
            public ulong free;
            public ulong used;
        }

        /// <summary>對應 nvmlUtilization_t：GPU 核心與顯存控制器使用率（百分比）。</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NvmlUtilization
        {
            public uint gpu;
            public uint memory;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
        private static extern int nvmlInit_v2();

        [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
        private static extern int nvmlShutdown();

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMemoryInfo")]
        private static extern int nvmlDeviceGetMemoryInfo(IntPtr device, ref NvmlMemory memory);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates")]
        private static extern int nvmlDeviceGetUtilizationRates(IntPtr device, ref NvmlUtilization utilization);
    }
}
