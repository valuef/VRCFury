using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;
using VF.Builder;
using VF.Feature.Base;
using VF.Inspector;
using VF.Model.Feature;

namespace VF.Feature {

    public class FullControllerBuilder : FeatureBuilder<FullController> {
        
        private Func<string,string> RewriteParamIfSynced;
        
        [FeatureBuilderAction]
        public void Apply() {
            var baseObject = model.rootObj != null ? model.rootObj : featureBaseObject;

            var syncedParams = new List<string>();
            if (model.parameters != null) {
                foreach (var param in model.parameters.parameters) {
                    if (string.IsNullOrWhiteSpace(param.name)) continue;
                    syncedParams.Add(param.name);
                    var newParam = new VRCExpressionParameters.Parameter {
                        name = RewriteParamName(param.name),
                        valueType = param.valueType,
                        saved = param.saved && !model.ignoreSaved,
                        defaultValue = param.defaultValue
                    };
                    prms.addSyncedParam(newParam);
                }
            }

            RewriteParamIfSynced = name => {
                if (syncedParams.Contains(name)) return RewriteParamName(name);
                return name;
            };

            if (model.controller != null) {
                AnimationClip RewriteClip(AnimationClip from) {
                    if (from == null) {
                        return controller.GetNoopClip();
                    }
                    var copy = controller.NewClip(baseObject.name + "__" + from.name);
                    motions.CopyWithAdjustedPrefixes(from, copy, baseObject);
                    return copy;
                }

                var merger = new ControllerMerger(
                    layerName => controller.NewLayerName("[FC" + uniqueModelNum + "_" + baseObject.name + "] " + layerName),
                    param => RewriteParamIfSynced(param),
                    RewriteClip
                );
                merger.Merge((AnimatorController)model.controller, controller.GetRawController());
            }

            if (model.menu != null) {
                string[] prefix;
                if (string.IsNullOrWhiteSpace(model.submenu)) {
                    prefix = new string[] { };
                } else {
                    prefix = model.submenu.Split('/').ToArray();
                }

                MergeMenu(prefix, model.menu);
            }

            if (model.toggleParam != null) {
                motions.Enable(defaultClip, baseObject, false);
                var enableLayer = controller.NewLayer("[FC" + uniqueModelNum + "_" + baseObject.name + "] VRCF Enabler");
                var off = enableLayer.NewState("Off");
                var onClip = controller.NewClip(baseObject.name + "__vrcfon");
                motions.Enable(onClip, baseObject);
                var on = enableLayer.NewState("On").WithAnimation(onClip);
                var toggle = controller.NewBool(RewriteParamName(model.toggleParam), usePrefix:false);
                off.TransitionsTo(on).When(toggle.IsTrue());
                on.TransitionsTo(off).When(toggle.IsFalse());
            }
        }

        private void MergeMenu(string[] prefix, VRCExpressionsMenu from) {
            foreach (var control in from.controls) {
                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null) {
                    var prefix2 = new List<string>(prefix);
                    prefix2.Add(control.name);
                    menu.GetMenu(prefix2.ToArray(), control.icon);
                    MergeMenu(prefix2.ToArray(), control.subMenu);
                } else {
                    menu.AddMenuItem(prefix, CloneControl(control));
                }
            }
        }

        private VRCExpressionsMenu.Control CloneControl(VRCExpressionsMenu.Control from) {
            return new VRCExpressionsMenu.Control {
                name = from.name,
                icon = from.icon,
                type = from.type,
                parameter = CloneControlParam(from.parameter),
                value = from.value,
                style = from.style,
                subMenu = from.subMenu,
                labels = from.labels,
                subParameters = from.subParameters == null ? null : new List<VRCExpressionsMenu.Control.Parameter>(from.subParameters)
                    .Select(CloneControlParam)
                    .ToArray(),
            };
        }
        private VRCExpressionsMenu.Control.Parameter CloneControlParam(VRCExpressionsMenu.Control.Parameter from) {
            if (from == null) return null;
            return new VRCExpressionsMenu.Control.Parameter {
                name = RewriteParamName(from.name)
            };
        }

        private string RewriteParamName(string name) {
            if (string.IsNullOrWhiteSpace(name)) return name;
            return controller.NewParamName("fc" + uniqueModelNum + "_" + name);
        }

        public override string GetEditorTitle() {
            return "Full Controller";
        }

        public override VisualElement CreateEditor(SerializedProperty prop) {
            var content = new VisualElement();
            content.Add(new PropertyField(prop.FindPropertyRelative("controller"), "Controller"));
            content.Add(new PropertyField(prop.FindPropertyRelative("menu"), "Menu"));
            content.Add(new PropertyField(prop.FindPropertyRelative("parameters"), "Params"));
            content.Add(VRCFuryEditorUtils.WrappedLabel("Submenu to place your menu's items within. If left empty, your menu will be merged " +
                                  "into the avatar's root menu."));
            content.Add(VRCFuryEditorUtils.PropWithoutLabel(prop.FindPropertyRelative("submenu")));
            return content;
        }
    }

}