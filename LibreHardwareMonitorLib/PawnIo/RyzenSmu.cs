using System;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.PawnIo;

public class RyzenSmu
{
    private readonly PawnIo _pawnIO = PawnIo.LoadModuleFromResource(typeof(IntelMsr).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.RyzenSMU.bin");

    public uint GetSmuVersion()
    {
        if (!Mutexes.WaitPciBus(5000))
            throw new TimeoutException("Timeout waiting for PCI bus mutex");

        uint version;

        try
        {
            long[] outArray = _pawnIO.Execute("ioctl_get_smu_version", [], 1);
            version = (uint)outArray[0];
        }
        finally
        {
            Mutexes.ReleasePciBus();
        }

        return version;
    }

    public long GetCodeName()
    {
        long[] outArray = _pawnIO.Execute("ioctl_get_code_name", [], 1);
        return outArray[0];
    }

    public long[] ReadPmTable(int size)
    {
        if (!Mutexes.WaitPciBus(5000))
            throw new TimeoutException("Timeout waiting for PCI bus mutex");

        try
        {
            long[] outArray = _pawnIO.Execute("ioctl_read_pm_table", [], size);
            return outArray;
        }
        finally
        {
            Mutexes.ReleasePciBus();
        }
    }

    public void UpdatePmTable()
    {
        if (!Mutexes.WaitPciBus(5000))
            throw new TimeoutException("Timeout waiting for PCI bus mutex");

        try
        {
            _pawnIO.Execute("ioctl_update_pm_table", [], 0);
        }
        finally
        {
            Mutexes.ReleasePciBus();
        }
    }

    public void ResolvePmTable(out uint version, out uint tableBase)
    {
        if (!Mutexes.WaitPciBus(5000))
            throw new TimeoutException("Timeout waiting for PCI bus mutex");

        try
        {
            long[] outArray = _pawnIO.Execute("ioctl_resolve_pm_table", [], 2);
            version = (uint)outArray[0];
            tableBase = (uint)outArray[1];
        }
        finally
        {
            Mutexes.ReleasePciBus();
        }
    }

    /// <summary>Set STAPM power limit in milliWatts.</summary>
    public bool SetStapmLimit(uint milliWatt) => ExecuteNoOutWithMutex("ioctl_set_stapm_limit", milliWatt);

    /// <summary>Set PPT fast (short) power limit in milliWatts.</summary>
    public bool SetPptFastLimit(uint milliWatt) => ExecuteNoOutWithMutex("ioctl_set_ppt_fast_limit", milliWatt);

    /// <summary>Set PPT slow (long/sustained) power limit in milliWatts.</summary>
    public bool SetPptSlowLimit(uint milliWatt) => ExecuteNoOutWithMutex("ioctl_set_ppt_slow_limit", milliWatt);

    /// <summary>Set GFX clock directly in MHz (if supported by the SMU on this family).</summary>
    public bool SetGfxClock(uint mhz) => ExecuteNoOutWithMutex("ioctl_set_gfx_clk", mhz);

    /// <summary>Set minimum GFX clock in MHz.</summary>
    public bool SetMinGfxClock(uint mhz) => ExecuteNoOutWithMutex("ioctl_set_min_gfxclk", mhz);

    /// <summary>Set maximum GFX clock in MHz.</summary>
    public bool SetMaxGfxClock(uint mhz) => ExecuteNoOutWithMutex("ioctl_set_max_gfxclk", mhz);

    // ------------ helper ------------
    private bool ExecuteNoOutWithMutex(string ioctl, params uint[] args)
    {
        // Convert to long[] once here (PawnIo.Execute signature uses long[]).
        long[] inArgs = new long[args.Length];
        for (int i = 0; i < args.Length; i++) inArgs[i] = args[i];

        if (!Mutexes.WaitPciBus(5000))
            throw new TimeoutException("Timeout waiting for PCI bus mutex");

        try
        {
            _pawnIO.Execute(ioctl, inArgs, 0);
            return true;
        }
        catch
        {
            // Execution failed (unsupported ioctl/family, driver not running, etc.)
            return false;
        }
        finally
        {
            Mutexes.ReleasePciBus();
        }
    }

    public void Close() => _pawnIO.Close();
}
