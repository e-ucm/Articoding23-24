/****************************************************************************

Copyright 2016 sophieml1989@gmail.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

****************************************************************************/

using Xasu.HighLevel; //articoding
using System.Xml; //articoding
using UnityEngine;
using UnityEngine.UI;

namespace UBlockly.UGUI
{
    public class VariableNameDialog : BaseDialog
    {
        [SerializeField] private Text m_InputLabel;
        [SerializeField] private InputField m_Input;
        private PanelControl inventoryControl; //articoding

        private bool mIsRename = false;
        
        private string mOldVarName;
        public void Rename(string varName)
        {
            mOldVarName = varName;
            mIsRename = true;
            m_InputLabel.text = I18n.Get(MsgDefine.RENAME_VARIABLE);
        }

        protected override void OnInit()
        {
            inventoryControl = GameObject.Find("OpenArea").GetComponent<PanelControl>(); //articoding
            m_InputLabel.text = I18n.Get(MsgDefine.NEW_VARIABLE);
            inventoryControl.DisableDissapear(true); //articoding

            AddCloseEvent(() =>
            {
                inventoryControl.DisableDissapear(false); //articoding
                if (mIsRename)
                {
                    BlocklyUI.WorkspaceView.Workspace.RenameVariable(mOldVarName, m_Input.text);

                    GameObjectTracker.Instance.Interacted("rename_variable") //articoding
                        .WithResultExtension("articoding://ext/new_variable_name", m_Input.text)
                        .WithResultExtension("articoding://ext/old_variable_name", mOldVarName)
                        .WithResultExtension("articoding://ext/block_type", "variable")
                        .WithResultExtension("articoding://ext/action", "rename")
                        .WithResultExtension("articoding://ext/level", GameManager.Instance.GetCurrentLevelName().ToLower());
                }
                else
                {
                    VariableModel model = BlocklyUI.WorkspaceView.Workspace.CreateVariable(m_Input.text);

                    if (model == null) return; //articoding

                    GameObjectTracker.Instance.Interacted("new_variable") //articoding
                        .WithResultExtension("articoding://ext/variable_name", model.Name)
                        .WithResultExtension("articoding://ext/block_type", "variable")
                        .WithResultExtension("articoding://ext/action", "declare")
                        .WithResultExtension("articoding://ext/level", GameManager.Instance.GetCurrentLevelName().ToLower());
                }

            });
        }

        private void OnDestroy()
        {
            inventoryControl.DisableDissapear(false);
        }
    }
}
