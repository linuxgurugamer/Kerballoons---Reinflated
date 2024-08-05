using KSP.UI.Screens;
using RUI.Icons.Selectable;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using ToolbarControl_NS;

using KSP_Log;

namespace Kerballoons
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KBFilter : MonoBehaviour
    {
        private readonly List<AvailablePart> parts = new List<AvailablePart>();
        internal string category = "Filter by function";
        internal bool filter = true;

        protected  string Manufacturer
        {
            get { return "KerBalloons"; }// part manufacturer in cfgs and agents files 
            set { }
        }
        protected  string categoryTitle
        {
            get { return "KerBalloons"; } // the category name 
            set { }
        }

        void Awake()
        {
            parts.Clear();
            var count = PartLoader.LoadedPartsList.Count;
            for (int i = 0; i < count; ++i)
            {
                var avPart = PartLoader.LoadedPartsList[i];
                if (!avPart.partPrefab) continue;
                if (avPart.manufacturer == Manufacturer)
                {
                    parts.Add(avPart);
                }
            }
            if (parts.Count > 0)
                GameEvents.onGUIEditorToolbarReady.Add(SubCategories);
            DontDestroyOnLoad(this);

        }

#if false
        void OnDestroy()
        {
            if (parts.Count > 0)
                GameEvents.onGUIEditorToolbarReady.Remove(SubCategories);

        }
#endif

        private bool EditorItemsFilter(AvailablePart avPart)
        {
            return parts.Contains(avPart);
        }

        private void SubCategories()
        {
            var icon = GetIcon(categoryTitle);
            var filter = PartCategorizer.Instance.filters.Find(f => f.button.categorydisplayName == "#autoLOC_453547");//change for 1.3.1
            PartCategorizer.AddCustomSubcategoryFilter(filter, categoryTitle, categoryTitle, icon, EditorItemsFilter);
        }

        private Icon GetIcon(string iconName)
        {
            Texture2D normIcon = new Texture2D(2, 2);
            if (!ToolbarControl.LoadImageFromFile(ref normIcon, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/../PluginData/" + iconName + "_off.png"))
            {
                Debug.Log("Error loading: " + iconName + "_off.png");
            }

            Texture2D selIcon = new Texture2D(2, 2);
            if (!ToolbarControl.LoadImageFromFile(ref selIcon, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/../PluginData/" + iconName + "_on.png"))
            {
                Debug.Log("Error loading: " + iconName + "_on.png");
            }

            Icon icon = new Icon(iconName + "Icon", normIcon, selIcon);

            return icon;

        }
    }

}