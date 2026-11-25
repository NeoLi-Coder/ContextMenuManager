using ContextMenuManager.Methods;
using iNKORE.UI.WPF.Modern.Controls;
using System;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface IChkVisibleItem
    {
        bool ItemVisible { get; set; }
        VisibleCheckBox ChkVisible { get; set; }
    }

    internal sealed class VisibleCheckBox : ToggleSwitch
    {
        public Func<bool> PreCheckChanging;

        private bool _loading;
        private bool _reverted;

        public VisibleCheckBox()
        {
            MinWidth = 0;
            Header = OnContent = OffContent = null;
        }

        public VisibleCheckBox(IChkVisibleItem item, bool isShellStoreDialog = false) : this()
        {
            var listItem = (MyListItem)item;
            _loading = true;
            Toggled += (s, e) =>
            {
                if (!_loading) item.ItemVisible = IsOn;
            };
            listItem.AddCtr(this);

            listItem.Control.Loaded += (s, e) =>
            {
                _loading = true;
                try
                {
                    IsOn = item.ItemVisible;
                    if (listItem is FoldSubItem subItem && subItem.FoldGroupItem != null) return;
                    if (isShellStoreDialog) return;
                    if (AppConfig.HideDisabledItems) listItem.Visible = IsOn;
                }
                finally
                {
                    _loading = false;
                }
            };
        }

        protected override void OnToggled()
        {
            // 同步显示状态时保留控件通知，不弹确认框，也不写回菜单。
            if (_loading)
            {
                base.OnToggled();
                return;
            }

            // We are reverting the toggle state
            if (_reverted)
            {
                _reverted = false;
                return;
            }

            if (PreCheckChanging == null || PreCheckChanging())
            {
                base.OnToggled();
            }
            // Revert the toggle state and do not invoke OnToggled again
            else if (!_reverted)
            {
                _reverted = true;
                IsOn = !IsOn;
            }
        }
    }
}
