using System.ComponentModel;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using CIIP.Module.BusinessObjects.Flow;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Validation;
using DevExpress.Persistent.BaseImpl;
using ���û���;
using DevExpress.ExpressApp;
using System.Collections.Generic;
using CIIP.Module.BusinessObjects.SYS.Logic;
using DevExpress.Data.Filtering;
using CIIP;
using System;
using System.Text;

namespace CIIP.Module.BusinessObjects.SYS
{
    public partial class BusinessObject : IDocumentProvider
    {
        public Guid GetDocumentGuid()
        {
            return this.Oid;
        }

        public string GetFileName()
        {
            return this.FullName;
        }

        public string GetCode()
        {
            var rst = new StringBuilder();
            rst.Append(CommonUsing());

            rst.AppendLine($@"namespace {Category.FullName}
{{");
            rst.AppendLine("\t//BO:" + Oid);
            if (!IsPersistent)
            {
                rst.AppendLine("\t[NonPersistent]");
            }

            if (IsCloneable.HasValue && IsCloneable.Value)
            {
                rst.AppendLine("\t[ModelDefault(\"Cloneable\",\"True\")]");
            }

            if (IsCreatableItem.HasValue && IsCreatableItem.Value)
            {
                rst.AppendLine("\t[ModelDefault(\"Createable\",\"True\")]");
            }

            if (IsVisibileInReports.HasValue && IsVisibileInReports.Value)
            {
                rst.AppendLine("\t[VisibleInReport]");
            }

            rst.Append($"\tpublic { (CanInherits ? "" : "sealed") + (IsAbstract ? "abstract" : "") } partial class { ���� } ");

            if (IsGenericTypeDefine)
            {
                //��������˷��Ͳ�����ֵ,����,������Ϊ����Ҳ�Ƿ�����
                rst.AppendFormat("<{0}>",
                    string.Join(",",
                        GenericParameters.Where(x => x.ParameterValue == null)
                            .OrderBy(x => x.ParameterIndex)
                            .Select(x => x.Name)
                            .ToArray()));
            }

            rst.Append(":");

            if (Base.IsGenericTypeDefine)
            {
                var n = Base.FullName;
                if (Base.IsRuntimeDefine)
                {
                    rst.Append("global::" + n);
                }
                else
                {
                    rst.Append("global::" + n.Substring(0, n.Length - 2));
                }

                //��������˷��Ͳ�����ֵ,����,������Ϊ����Ҳ�Ƿ�����
                rst.AppendFormat("<{0}>",
                    string.Join(",",
                        GenericParameters.Select(
                            x => x.ParameterValue == null ? x.Name : "global::" + x.ParameterValue.FullName).ToArray()));
            }
            else
            {
                rst.Append("global::" + Base.FullName);
            }
            rst.AppendLine();

            var constraints = string.Join("\n", GenericParameters.Where(x => !string.IsNullOrEmpty(x.Constraint)).Select(x => " where " + x.Name + " : " + x.Constraint));
            rst.AppendLine(constraints);

            //begin class
            rst.AppendLine("\t{");

            rst.AppendLine($"\t\tpublic {����}(Session s):base(s){{  }}");

            var propertyTemplate =
"\t\tpublic {0} {1}\n" +
"\t\t{\n" +
"\t\t\tget { return _{1}; }\n" +
"\t\t\tset { SetPropertyValue(\"{1}\",ref _{1},value); }\n" +
"\t\t}\n";
            foreach (var item in Properties)
            {
                var pt = "global::" + item.PropertyType.FullName;
                rst.AppendLine($"\t\t{ pt } _{ item.���� };");

                if (item.Size != 100 && item.Size != 0)
                {
                    rst.AppendLine($"\t\t[Size({item.Size})]");
                }
                ProcessPropertyBase(rst, item);
                if (item.RelationProperty != null)
                {
                    var assName = string.Format("{0}_{1}", item.RelationProperty.����, item.����);
                    rst.AppendFormat("\t\t[{0}(\"{1}\")]", typeof(AssociationAttribute).FullName, assName);
                }


                rst.Append(propertyTemplate.Replace("{0}", pt).Replace("{1}", item.����));
            }

            var att = "\t\t[" + typeof(DevExpress.Xpo.AggregatedAttribute).FullName + "]";
            foreach (var item in CollectionProperties)
            {
                if (item.Aggregated)
                {
                    rst.AppendLine(att);
                }
                ProcessPropertyBase(rst, item);

                var assName = string.Format("{0}_{1}", item.����, item.RelationProperty.����);
                rst.AppendFormat("\t\t[{0}(\"{1}\")]\n", typeof(AssociationAttribute).FullName, assName);
                var pt = "global::" + item.PropertyType.FullName;
                rst.AppendLine($"\t\tpublic XPCollection<{pt}> {item.����}{{ get{{ return GetCollection<{pt}>(\"{item.����}\"); }} }}");
            }

            foreach (var method in Methods)
            {
                rst.AppendLine(method.MethodDefineCode);
            }

            //end class
            rst.AppendLine("\t}");
            rst.AppendLine("}");
            return rst.ToString();
        }
        public void ProcessPropertyBase(StringBuilder code, PropertyBase property)
        {
            if (!string.IsNullOrEmpty(property.DataSourceProperty))
            {
                code.AppendFormat("\t\t[{0}(\"{1}\")]\n", typeof(DataSourcePropertyAttribute).FullName, property.DataSourceProperty);
            }
            
            if (property.VisibleInDetailView.HasValue && !property.VisibleInDetailView.Value)
            {
                code.AppendFormat("\t\t[{0}(false)]\n", typeof(VisibleInDetailViewAttribute).FullName);
            }

            if (property.VisibleInListView.HasValue && !property.VisibleInListView.Value)
            {
                code.AppendFormat("\t\t[{0}(false)]\n", typeof(VisibleInListViewAttribute).FullName);
            }

            if (property.Browsable.HasValue && !property.Browsable.Value)
            {
                code.AppendFormat("\t\t[{0}(false)]\n", typeof(BrowsableAttribute).FullName);
            }

            if (property.AllowEdit.HasValue && !property.AllowEdit.Value)
            {
                code.AppendFormat("\t\t[ModelDefault(\"AllowEdit\",\"false\")]\n");
            }

            if (property.ImmediatePostData.HasValue && property.ImmediatePostData.Value)
            {
                code.AppendFormat("\t\t[ImmediatePostData]\n");
            }

            if(!string.IsNullOrEmpty( property.DisplayFormat ))
            {
                code.AppendFormat("\t\t[ModelDefault(\"DisplayFormat\",\"{0}\")]\n",property.DisplayFormat);
            }

            if (!string.IsNullOrEmpty(property.EditMask))
            {
                code.AppendFormat("\t\t[ModelDefault(\"EditMask\",\"{0}\")]\n", property.EditMask);
            }

            if (property.Range != null)
            {
                code.AppendFormat("\t\t[RuleRange({0},{1})]\n", property.Range.Begin, property.Range.End);
            }

            if (property.RuleRequiredField.HasValue && property.RuleRequiredField.Value)
            {
                code.AppendFormat("\t\t[RuleRequiredField]\n");
            }

            if (property.UniqueValue.HasValue && property.UniqueValue.Value)
            {
                code.AppendFormat("\t\t[RuleUniqueValue]\n");
            }
        }
    }
    
    [XafDefaultProperty("Caption")]
    [XafDisplayName("�û�ҵ��")]
    [LookupEditorMode(LookupEditorMode.AllItemsWithSearch)]
    //[ChildrenType(typeof(MethodDefine))]
    public partial class BusinessObject : BusinessObjectBase
    {
        public static string CommonUsing()
        {
            return @"using System;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.General;
using DevExpress.Xpo;
using System.Linq;
using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using ���û���;
using ������Ϣ;
using DevExpress.Persistent.Validation;
";
        }

        private bool _CanCustomLogic;
        [XafDisplayName("���Զ����߼�")]
        [ModelDefault("AllowEdit","False")]
        public bool CanCustomLogic
        {
            get { return _CanCustomLogic; }
            set { SetPropertyValue("CanCustomLogic", ref _CanCustomLogic, value); }
        }

        private bool _IsPersistent;

        [XafDisplayName("�ɳ־û�")]
        [ToolTip("���Ƿ������ݿ��д����������Խ��ж�д��������ǳ־û��ģ���ֻ��Ϊ�������ʱʹ��.")]
        [VisibleInListView(false)]
        public bool IsPersistent
        {
            get { return _IsPersistent; }
            set { SetPropertyValue("IsPersistent", ref _IsPersistent, value); }
        }

        private bool _CanInherits;

        [XafDisplayName("�ɱ��̳�")]
        [VisibleInListView(false)]
        public bool CanInherits
        {
            get { return _CanInherits; }
            set { SetPropertyValue("CanInherits", ref _CanInherits, value); }
        }

        private bool _IsAbstract;

        [XafDisplayName("�������")]
        [ToolTip("ָ�������ǲ����Ա�����ʵ���ģ������ڼ̳�ʱʹ��.")]
        [VisibleInListView(false)]
        public bool IsAbstract
        {
            get { return _IsAbstract; }
            set
            {
                SetPropertyValue("IsAbstract", ref _IsAbstract, value);

                if (!IsLoading)
                {
                    if (value)
                    {
                        CanInherits = true;
                    }
                }
            }
        }

#warning ����ʹ��IsSystem��������
        /// <summary>
        /// �������Զ�����ϵͳ����ʱ����Ҫ�Զ����ɷ��Ͳ���
        /// </summary>
        [Browsable(false)]
        [NonPersistent]
        public bool DisableCreateGenericParameterValues;

        private BusinessObject _Base;

        [XafDisplayName("�̳�")]
        [RuleRequiredField]
        [LookupEditorMode(LookupEditorMode.AllItemsWithSearch)]
        public BusinessObject Base
        {
            get { return _Base; }
            set
            {
                SetPropertyValue("Base", ref _Base, value);
            }
        }

        private bool _IsGenericTypeDefine;
        [XafDisplayName("���Ͷ���")]
        public bool IsGenericTypeDefine
        {
            get { return _IsGenericTypeDefine; }
            set { SetPropertyValue("IsGenericTypeDefine", ref _IsGenericTypeDefine, value); }
        }
        
        //[Appearance("���෺�Ͳ����ɼ�", TargetItems = "GenericParameters", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide)]
        protected bool GenericParameterIsVisible()
        {
            if (Base != null)
            {
                if (Base.IsRuntimeDefine)
                    return true;
                var bt = ReflectionHelper.FindType(Base.FullName);
                if (bt != null)
                {
                    return !bt.IsGenericType;
                }
            }
            return true;
        }
        
        [Association, DevExpress.Xpo.Aggregated]
        [XafDisplayName("����")]
        public XPCollection<Property> Properties
        {
            get { return GetCollection<Property>("Properties"); }
        }

        [Association, DevExpress.Xpo.Aggregated]
        [XafDisplayName("��������")]
        public XPCollection<CollectionProperty> CollectionProperties
        {
            get { return GetCollection<CollectionProperty>("CollectionProperties"); }
        }

        private bool? _IsCloneable;

        [XafDisplayName("��������")]
        [VisibleInListView(false)]
        public bool? IsCloneable
        {
            get { return _IsCloneable; }
            set { SetPropertyValue("IsCloneable", ref _IsCloneable, value); }
        }

        private bool? _IsVisibileInReports;

        [XafDisplayName("��������")]
        [VisibleInListView(false)]
        public bool? IsVisibileInReports
        {
            get { return _IsVisibileInReports; }
            set { SetPropertyValue("IsVisibileInReports", ref _IsVisibileInReports, value); }
        }

        private bool? _IsCreatableItem;

        [XafDisplayName("���ٴ���")]
        [VisibleInListView(false)]
        public bool? IsCreatableItem
        {
            get { return _IsCreatableItem; }
            set { SetPropertyValue("IsCreatableItem", ref _IsCreatableItem, value); }
        }

        private bool _IsRuntimeDefine;

        [XafDisplayName("��̬����")]
        [ToolTip("Ϊ��ʱ��ͨ�����뷽ʽ�ϴ���ģ�����ɵġ��������ڽ����϶��岢���ɵġ�")]
        public bool IsRuntimeDefine
        {
            get { return _IsRuntimeDefine; }
            set { SetPropertyValue("IsRuntimeDefine", ref _IsRuntimeDefine, value); }
        }

        [Browsable(false)]
        public int CreateIndex { get; set; }

        public BusinessObject(Session s) : base(s)
        {

        }

        protected override void OnChanged(string propertyName, object oldValue, object newValue)
        {
            base.OnChanged(propertyName, oldValue, newValue);
            if (!IsLoading && (propertyName == "����" || propertyName == "Category"))
            {
                if (this.Category != null)
                    this.FullName = this.Category.FullName + "." + this.����;
                if (propertyName == "����" && string.IsNullOrEmpty(Caption))
                {
                    this.Caption = newValue + "";
                }
            }
            if (propertyName == "Base" && !IsLoading && !DisableCreateGenericParameterValues)
            {
                Session.Delete(GenericParameters);
                if (newValue != null)
                {
                    if (!Base.IsRuntimeDefine)
                    {
                        var bt = ReflectionHelper.FindType(Base.FullName);
                        if (bt.IsGenericType)
                        {
                            foreach (var item in bt.GetGenericArguments())
                            {
                                var gp = new GenericParameter(Session);
                                gp.Owner = this;
                                gp.Name = item.Name;
                            }
                        }
                    }
                    else
                    {
                        foreach (var gp in Base.GenericParameters)
                        {
                            var ngp = new GenericParameter(Session);
                            ngp.Name = gp.Name;
                            ngp.ParameterIndex = gp.ParameterIndex;
                            this.GenericParameters.Add(ngp);
                        }
                    }

                }
            }
        }

        public override void AfterConstruction()
        {
            IsRuntimeDefine = true;
            this.IsPersistent = true;
            this.CanInherits = true;
            base.AfterConstruction();
        }

        public PropertyBase FindProperty(string name)
        {
            var sp = Properties.SingleOrDefault(x => x.���� == name);
            if (sp != null)
            {
                return sp;
            }
            return CollectionProperties.SingleOrDefault(x => x.���� == name);
        }

        [Association, DevExpress.Xpo.Aggregated]
        [XafDisplayName("������෺�Ͳ���")]
        public XPCollection<GenericParameter> GenericParameters
        {
            get { return GetCollection<GenericParameter>("GenericParameters"); }
        }

        List<NavigationItem> NavigationItemDataSources
        {
            get
            {
                return ModelDataSource.NavigationItemDataSources;
            }
        }

        private NavigationItem _NavigationItem;
        [ValueConverter(typeof(ModelNavigationToStringConverter))]
        [DataSourceProperty("NavigationItemDataSources")]
        [XafDisplayName("����")]
        public NavigationItem NavigationItem
        {
            get { return _NavigationItem; }
            set { SetPropertyValue("NavigationItem", ref _NavigationItem, value); }
        }
        
        //[Association, DevExpress.Xpo.Aggregated]
        //public XPCollection<MethodDefine> Methods
        //{
        //    get
        //    {
        //        return GetCollection<MethodDefine>("Methods");
        //    }
        //}

        [Association, DevExpress.Xpo.Aggregated]
        [XafDisplayName("ҵ���߼�")]
        public XPCollection<BusinessObjectEvent> Methods
        {
            get
            {
                return GetCollection<BusinessObjectEvent>("Methods");
            }
        }

        //private bool _UserDBView;
        //[XafDisplayName("ʹ��DB��ͼ")]
        //public bool UserDBView
        //{
        //    get { return _UserDBView; }
        //    set { SetPropertyValue("UserDBView", ref _UserDBView, value); }
        //}



        //private string _MapToDBView;
        //[Size(-1)]
        //[XafDisplayName("ӳ�䵽DB��ͼ")]
        //[ToolTip("ϵͳ��ʹ�������SQL��䴴����ͼ����ҪдCreate View!")]
        //public string MapToDBView
        //{
        //    get
        //    {
                
        //        return _MapToDBView;
        //    }
        //    set
        //    {
        //        SetPropertyValue("MapToDBView", ref _MapToDBView, value);
        //    }
        //}



        #region quick add
        public Property AddProperty(string name, BusinessObjectBase type, int? size = null)
        {
            var property = new Property(Session);
            property.PropertyType = type;
            property.���� = name;
            if (size.HasValue)
                property.Size = size.Value;
            this.Properties.Add(property);
            return property;
        }

        public Property AddProperty<T>(string name, int? size = null)
        {
            return AddProperty(name,
                Session.FindObject<BusinessObjectBase>(new BinaryOperator("FullName", typeof (T).FullName))
                , size
                );
        }

        public CollectionProperty AddAssociation(string name, BusinessObject bo, bool isAggregated,Property relation)
        {
            var cp = new CollectionProperty(Session);
            this.CollectionProperties.Add(cp);
            cp.PropertyType = bo;
            cp.���� = name;
            cp.Aggregated = isAggregated;
            cp.RelationProperty = relation;
            return cp;
        }

        public ObjectAfterConstruction AddAfterConstruction(string code)
        {
            var after = new ObjectAfterConstruction(Session);
            after.SetCode(code);
            Methods.Add(after);
            return after;
        }

        public ObjectSavingEvent AddSavingEvent(string code)
        {
            var after = new ObjectSavingEvent(Session);
            after.SetCode(code);
            Methods.Add(after);
            return after;
        }

        public ObjectDeletingEvent AddDeletingEvent(string code)
        {
            var after = new ObjectDeletingEvent(Session);
            after.SetCode(code);
            Methods.Add(after);
            return after;
        }

        public ObjectPropertyChangedEvent AddPropertyChangedEvent(string code)
        {
            var after = new ObjectPropertyChangedEvent(Session);
            after.SetCode(code);
            Methods.Add(after);
            return after;
        }
        public ObjectSavedEvent AddObjectSavedEvent(string code)
        {
            var after = new ObjectSavedEvent(Session);
            after.SetCode(code);
            Methods.Add(after);
            return after;
        }

        public BusinessObjectPartialLogic AddPartialLogic(string code,string usingBlock = null)
        {
            var logic = new BusinessObjectPartialLogic(Session);
            logic.BusinessObject = this;
            if (usingBlock == null)
            {
                code = CommonUsing() + code;
            }
            logic.Code = new CsharpCode(code, logic);
            return logic;
        }

        public BusinessObjectLayout AddLayout(string code, string usingBlock = null)
        {
            var logic = new BusinessObjectLayout(Session);
            logic.BusinessObject = this;
            if (usingBlock == null)
            {
                code = CommonUsing() + code;
            }
            logic.Code = new CsharpCode(code, logic);
            return logic;
        }

        #endregion

        

#warning ��Ҫ��֤�������Ʋ��������������.
    }


#warning �˹��ܿ��Ժ���ʵ��,��ǰ����ʹ�ø��ƹ���ֱ��copy���в���
    // ҵ����������,ʹ��Attributeָ��ʹ���ĸ�����ģ��
    // ϵͳ��ʱ,�������ʹ����Attribute����,���������и���

    //[LayoutTemplate(typeof(����ģ��)] 
    //���Ͳ�������Ӧ����: ĳ����,������ϸ ��������.
    //�������,ֻ֧����������,����������ж������,�Ͱ�˳����,����ȡ��,���账��.
    //public class ĳ���� :  ......
    //{
    //}
}