using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.DropdownSystem;
using Timberborn.Goods;
using Timberborn.GoodsUI;
using Timberborn.Localization;
using UnityEngine;

namespace Calloatti.StorageMonitor
{
  internal class StorageMonitorGoodsDropdownProvider : BaseComponent, IAwakableComponent, IStartableComponent, IExtendedTooltipDropdownProvider, IExtendedDropdownProvider, IDropdownProvider
  {
    private static readonly string AutomationNoneLocKey = "Automation.AutomationNone";

    private readonly IGoodService _goodService;
    private readonly GoodDescriber _goodDescriber;
    private readonly ILoc _loc;
    private StorageMonitor _storageMonitor;

    public IReadOnlyList<string> Items { get; private set; }

    public StorageMonitorGoodsDropdownProvider(IGoodService goodService, GoodDescriber goodDescriber, ILoc loc)
    {
      _goodService = goodService;
      _goodDescriber = goodDescriber;
      _loc = loc;
    }

    public void Awake()
    {
      _storageMonitor = GetComponent<StorageMonitor>();
    }

    public void Start()
    {
      Items = _goodService.Goods.OrderBy((string good) => FormatDisplayText(good, selected: false)).ToImmutableArray();
    }

    public string GetValue()
    {
      string currentGood = _storageMonitor.GoodId;

      if (string.IsNullOrEmpty(currentGood) && Items != null && Items.Count > 0)
      {
        currentGood = Items.Contains("Water") ? "Water" : Items[0];
        _storageMonitor.SetGoodId(currentGood);
      }

      return currentGood;
    }

    public void SetValue(string goodId)
    {
      if (!string.IsNullOrEmpty(goodId))
      {
        _storageMonitor.SetGoodId(goodId);
      }
    }

    public string FormatDisplayText(string goodId, bool selected)
    {
      if (string.IsNullOrEmpty(goodId)) return _loc.T(AutomationNoneLocKey);

      return _goodService.GetGood(goodId).DisplayName.Value;
    }

    public Sprite GetIcon(string goodId)
    {
      if (string.IsNullOrEmpty(goodId)) return null;
      return _goodDescriber.GetIcon(goodId);
    }

    public ImmutableArray<string> GetItemClasses(string value) => ImmutableArray<string>.Empty;

    public string GetDropdownTooltip(string value) => FormatDisplayText(value, selected: false);
  }
}