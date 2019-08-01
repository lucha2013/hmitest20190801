/*
 * Created by SharpDevelop.
 * User: admin
 * Date: 2018/3/18 星期日
 * Time: 15:13
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace KvControl
{
	/// <summary>
	/// Interface for controller
	/// </summary>
	public interface IKvController
	{
		/// <summary>
		/// EM300
		/// </summary>
		string KVMemAddr{get;set;}
		/// <summary>
		/// 32|16
		/// </summary>
		int KVByteLen{get;set;}		
		/// <summary>
		/// Sync with plc
		/// </summary>
		bool KVAutoSync{get;set;}
		/// <summary>
		/// Support only read or write
		/// </summary>
		bool KVReadOnly{get;set;}
		/// <summary>
		/// RD or RDS
		/// </summary>
		/// <returns></returns>
		string GetReadCmd();
		/// <summary>
		/// WR or WRS
		/// </summary>
		/// <returns></returns>
		string GetWriteCmd();
		
		void UpdateUI(string response);
	}
	
}
