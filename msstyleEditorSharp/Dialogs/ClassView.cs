﻿using libmsstyle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace msstyleEditor.Dialogs
{
    public partial class ClassViewWindow : ToolWindow
    {
        private VisualStyle m_style = null;

        public event TreeViewEventHandler OnSelectionChanged;

        public ClassViewWindow()
        {
            InitializeComponent();
            classView.TreeViewNodeSorter = new TreeRootSorter();
        }

        public void SetVisualStyle(VisualStyle style)
        {
            m_style = style;

            classView.BeginUpdate();
            if (m_style == null)
            {
                classView.Nodes.Clear();
            }
            else
            {
                foreach (var cls in m_style.Classes)
                {
                    var clsNode = new TreeNode(cls.Value.ClassName);
                    clsNode.Tag = cls.Value;

                    foreach (var part in cls.Value.Parts)
                    {
                        var partNode = new TreeNode(part.Value.PartName);
                        partNode.Tag = part.Value;

                        var ctxMenu = new ContextMenuStrip();
                        ToolStripMenuItem copyLabel = new ToolStripMenuItem
                        {
                            Text = "Copy"
                        };
                        copyLabel.Click += (object sender, EventArgs e) =>
                        {
                            Clipboard.SetData(typeof(StylePart).FullName, partNode.Tag);
                        };
                        ToolStripMenuItem pasteLabel = new ToolStripMenuItem
                        {
                            Text = "Paste"
                        };
                        pasteLabel.Click += (object sender, EventArgs e) =>
                        {
                            if (Clipboard.GetData(typeof(StylePart).FullName) is StylePart data)
                            {
                                StylePart current = partNode.Tag as StylePart;

                                foreach (var state in data.States)
                                {
                                    if (current.States.ContainsKey(state.Key))
                                    {
                                        foreach (var prop in state.Value.Properties)
                                        {
                                            current.States[state.Key].Properties.RemoveAll(v => v.Header.nameID == prop.Header.nameID);
                                            StyleProperty p = prop;
                                            p.Header.classID = cls.Value.ClassId;
                                            p.Header.partID = current.PartId;
                                            current.States[state.Key].Properties.Add(p);
                                        }
                                    }
                                }
                            }
                        };

                        ctxMenu.Items.AddRange(new ToolStripMenuItem[] { copyLabel, pasteLabel });

                        partNode.ContextMenuStrip = ctxMenu;

                        clsNode.Nodes.Add(partNode);
                    }
                    classView.Nodes.Add(clsNode);
                }
                classView.Sort();
            }
            classView.EndUpdate();
        }

        bool m_endReached = false;
        public void FindNextNode(SearchDialog.SearchMode mode, IDENTIFIER type, string searchString, object searchObject)
        {
            var startItem = classView.SelectedNode;
            if (startItem == null || m_endReached)
            {
                m_endReached = false;
                if (classView.Nodes.Count > 0)
                {
                    startItem = classView.Nodes[0];
                }
                else return;
            }

            var node = TreeViewSearch.FindNextNode(classView, startItem, (_node) =>
            {
                switch (mode)
                {
                    case SearchDialog.SearchMode.Name:
                        return _node.Text.ToUpper().Contains(searchString.ToUpper());
                    case SearchDialog.SearchMode.Property:
                        {
                            StylePart part = _node.Tag as StylePart;
                            if (part != null)
                            {
                                return part.States.Any((kvp) =>
                                {
                                    return kvp.Value.Properties.Any((p) =>
                                    {
                                        return p.Header.typeID == (int)type &&
                                               p.GetValue().Equals(searchObject);
                                    });
                                });
                            }
                        }
                        return false;
                    default: return false;
                }
            });

            if (node != null)
            {
                classView.SelectedNode = node;
            }
            else
            {
                MessageBox.Show($"No further match for \"{searchString}\" !\nSearch will begin from top again.", ""
                    , MessageBoxButtons.OK
                    , MessageBoxIcon.Information);
                m_endReached = true;
            }
        }

        public void ExpandAll()
        {
            classView.ExpandAll();
        }

        public void CollapseAll()
        {
            classView.CollapseAll();
        }

        private void OnTreeItemSelected(object sender, TreeViewEventArgs e)
        {
            if(OnSelectionChanged != null)
            {
                OnSelectionChanged(sender, e);
            }
        }
    }
}
