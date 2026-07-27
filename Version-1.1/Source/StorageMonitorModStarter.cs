using System;
using Timberborn.Modding;
using Timberborn.ModManagerScene;

namespace Calloatti.StorageMonitor
{
  public class StorageMonitorModStarter : IModStarter
  {
    public void StartMod(IModEnvironment modEnvironment)
    {
      Console.WriteLine("[Calloatti.StorageMonitor] Mod started successfully.");
    }
  }
}