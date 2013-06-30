using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace AutoCore.CoreMode
{
    /// <summary>
    /// 测试逻辑
    /// </summary>
    public interface ILogic
    {
        /// <summary>
        /// 返回测试框架
        /// </summary>
        Core GtrCore { get; }

        /// <summary>
        /// 返回测试逻辑所在的程序集
        /// </summary>
        Assembly CurrAssembly { get; }

        /// <summary>
        /// 测试逻辑所在的测试集
        /// </summary>
        ITestCluster iTestCluster { get; set; }

        /// <summary>
        /// 测试逻辑的类型
        /// </summary>
        Type TestClusterType { get; }

        /// <summary>
        /// 测试逻辑的测试数据
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string>[] Data();

        /// <summary>
        /// 测试逻辑的预处理
        /// </summary>
        /// <returns></returns>
        bool SetupScripts();

        /// <summary>
        /// 测试逻辑
        /// </summary>
        /// <param name="data"></param>
        void AwLogic(Dictionary<string, string> data);

        /// <summary>
        /// 测试逻辑每次执行完之后进行恢复环境处理
        /// </summary>
        /// <param name="data"></param>
        void RestoreEnv(Dictionary<string, string> data);

        /// <summary>
        /// 当前测试逻辑执行完之后进行恢复处理
        /// </summary>
        /// <returns></returns>
        bool RestoreScripts();
    }

    /// <summary>
    /// 测试逻辑的详情信息
    /// </summary>
    public class LogicInfo : IComparable 
    {
        /// <summary>
        /// 当前测试逻辑
        /// </summary>
        private ILogic Logic = null;

        /// <summary>
        /// 测试逻辑详细信息的构造方法
        /// </summary>
        /// <param name="_Logic"></param>
        public LogicInfo(ILogic _Logic)
        {
            this.Logic = _Logic;
        }

        /// <summary>
        /// 返回当前的测试逻辑
        /// </summary>
        public ILogic logic { get { return this.Logic; } }

        /// <summary>
        /// 比较当前测试逻辑所在的测试集是否与另外一个测试逻辑的测试集相同
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            var p = obj as LogicInfo;
            if (p == null)
                throw new Exception("obj not Type of LogicInfo");

            //logic.TestClusterType.FullName 是否与 logic.iTestCluster.GetType().FullName一样
            //似乎这里的意思是 判断是否继承于相同的模板.
            return this.logic.TestClusterType.FullName.CompareTo(p.logic.TestClusterType.FullName);
        }
    }

    /// <summary>
    /// 测试模板类
    /// </summary>
    /// <typeparam name="T">该测试逻辑所属的测试集</typeparam>
    public abstract class LogicBase<T> : ILogic where T : TestCluster
    {
        /// <summary>
        /// 返回当前的自动化框架
        /// </summary>
        public Core GtrCore
        {
            get { return Core.GetInstance(); }
        }

        /// <summary>
        /// 测试逻辑所在的程序集
        /// </summary>
        public Assembly CurrAssembly 
        { get { return Assembly.GetAssembly(this.GetType()); } }

        /// <summary>
        /// 测试逻辑所属的测试集
        /// </summary>
        public ITestCluster iTestCluster
        {
            get;
            set;
        }

        public T TestCluster
        {
            get
            {
                return (T)iTestCluster;
            }
        }

        /// <summary>
        /// 测试逻辑所属的测试集的类型
        /// </summary>
        public Type TestClusterType { get { return typeof(T); } }

        /// <summary>
        /// 测试逻辑的测试数据
        /// </summary>
        /// <returns></returns>
        public abstract Dictionary<string, string>[] Data();

        /// <summary>
        /// 测试逻辑的初始化
        /// </summary>
        /// <returns></returns>
        public virtual bool  SetupScripts()
        {
            return true;
        }

        /// <summary>
        /// 测试逻辑的环境恢复
        /// </summary>
        /// <returns></returns>
        public virtual bool RestoreScripts()
        {
            return true;
        }

        /// <summary>
        /// 测试逻辑
        /// </summary>
        /// <param name="data"></param>
        public virtual void AwLogic(Dictionary<string, string> data) { }

        /// <summary>
        /// 测试逻辑在每次执行完测试数据之后恢复环境
        /// </summary>
        /// <param name="data"></param>
        public virtual void RestoreEnv(Dictionary<string, string> data) { }
    }

}
