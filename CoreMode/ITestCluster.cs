using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCore.CoreMode
{
    /// <summary>
    /// 测试集接口
    /// </summary>
    public interface ITestCluster
    {
        /// <summary>
        /// 返回测试框架类
        /// </summary>
        Core GtrCore { get; }

        /// <summary>
        /// 测试集的初始化工作
        /// </summary>
        /// <returns></returns>
        bool SetupScripts();

        /// <summary>
        /// 测试集的恢复环境工作
        /// </summary>
        /// <returns></returns>
        bool RestoreScripts();
    }

    public abstract class TestCluster : ITestCluster
    {
        /*一个测试集必须包含建立脚本环境
         还有恢复脚本环境*/
        public TestCluster() { }

        /// <summary>
        /// Gtr执行的平台
        /// </summary>
        /// <returns></returns>
        public Core GtrCore
        {
            get
            {
                return Core.GetInstance();
            }
        }

        public virtual bool SetupScripts()
        {
            return true;
        }

        public virtual bool RestoreScripts()
        {
            return true;
        }

        public int CompareTo(object obj)
        {
            var p = obj as TestCluster;
            if (p == null)
                throw new Exception("Type not compared.");

            return this.GetType().FullName.CompareTo(p.GetType().FullName);
        }
    }
}
