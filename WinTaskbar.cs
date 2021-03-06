// TaskProgressBar (Portable)
// Edited By RyuaNerin

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;

namespace WinTaskbar
{
	public enum TaskbarProgressBarState
	{
		NoProgress = 0x00,
		/// <summary>Color : Green Marquee</summary>
		Indeterminate = 0x01,
		/// <summary>Color : Green</summary>
		Normal = 0x02,
		/// <summary>Color : Red</summary>
		Error = 0x04,
		/// <summary>Color : Yellow</summary>
		Paused = 0x08
	}

	public class Taskbar
	{
		static Taskbar()
		{
			Version version = Environment.OSVersion.Version;
			Taskbar.m_isWin7 = (version.Major > 6) || (version.Major == 6 && version.Minor >= 1);
		}

		protected static readonly bool m_isWin7;

		//////////////////////////////////////////////////////////////////////////

		protected IntPtr m_owner = IntPtr.Zero;
		protected WinAPIs.ITaskbarList4 m_taskbarList = null;
		protected TaskbarProgress m_taskbarProgress = null;

		public Taskbar(Form ownerForm) : this()
		{
			this.m_owner = ownerForm.Handle;
		}
		public Taskbar(IntPtr ownerPtr) : this()
		{
			this.m_owner = ownerPtr;
		}
		private Taskbar()
		{
			if (Taskbar.m_isWin7)
			{
				this.m_taskbarList = (WinAPIs.ITaskbarList4)new WinAPIs.CTaskbarList();
				m_taskbarList.HrInit();
			}

			this.m_taskbarProgress = new TaskbarProgress(this);
		}

		public WinAPIs.ITaskbarList4 TaskbarList
		{
			get { return this.m_taskbarList; }
		}

		public TaskbarProgress ProgressBar
		{
			get { return this.m_taskbarProgress; }
		}

		public IntPtr OwnerHandle
		{
			get { return this.m_owner; }
		}

		#region ProgressBar
		protected int m_minimum = 0;
		protected int m_value = 0;
		protected int m_maximum = 100;
		protected TaskbarProgressBarState m_state = TaskbarProgressBarState.NoProgress;

		public class TaskbarProgress
		{
			private readonly Taskbar m_taskbar;

			internal TaskbarProgress(Taskbar taskbar)
			{
				m_taskbar = taskbar;
			}

			public int Minimum
			{
				get { return this.m_taskbar.m_minimum; }
				set
				{
					if (value < this.Maximum)
						throw new ArgumentOutOfRangeException("Minimum must be smaller or same than Maximum");

					this.m_taskbar.m_minimum = value;

					if (this.m_taskbar.m_value < this.m_taskbar.m_minimum)
						this.m_taskbar.m_value = this.m_taskbar.m_minimum;

					this.m_taskbar.SetProgressValue();
				}
			}

			public int Value
			{
				get { return this.m_taskbar.m_value; }
				set
				{
					if (value < this.m_taskbar.m_minimum)
						throw new ArgumentOutOfRangeException("Value must be bigger or same than Minimum");

					if (value > this.m_taskbar.m_maximum)
						throw new ArgumentOutOfRangeException("Value must be smaller or same than Maximum");

					this.m_taskbar.m_value = value;

					this.m_taskbar.SetProgressValue();
				}
			}

			public int Maximum
			{
				get { return this.m_taskbar.m_maximum; }
				set
				{
					if (value < this.m_taskbar.m_minimum)
						throw new ArgumentOutOfRangeException("Value must be smaller or same than Maximum");

					this.m_taskbar.m_maximum = value;

					if (this.m_taskbar.m_value > this.m_taskbar.m_maximum)
						this.m_taskbar.m_value = this.m_taskbar.m_maximum;

					this.m_taskbar.SetProgressValue();
				}
			}

			public TaskbarProgressBarState State
			{
				get { return this.m_taskbar.m_state; }
				set
				{
					this.m_taskbar.m_state = value;
					this.m_taskbar.SetProgressState(this.m_taskbar.m_state);
				}
			}
		}

		public void SetProgressValue(int minimumValue, int currentValue, int maximumValue)
		{
			if (!Taskbar.m_isWin7) return;

			if (currentValue < minimumValue)
				throw new ArgumentOutOfRangeException("currentValue must be same or bigger than minimumvalue");

			if (maximumValue < currentValue)
				throw new ArgumentOutOfRangeException("maximumValue must be same or bigger than currentValue");

			if (maximumValue < minimumValue)
				throw new ArgumentOutOfRangeException("maximumValue must be same or bigger than minimumValue");

			this.m_minimum = minimumValue;
			this.m_value = currentValue;
			this.m_maximum = maximumValue;

			this.SetProgressValue();
		}

		public void SetProgressValue(int currentValue, int maximumValue)
		{
			if (maximumValue < currentValue)
				throw new ArgumentOutOfRangeException("maximumValue must be same or bigger than currentValue");

			this.m_value = currentValue + this.m_minimum;
			this.m_maximum = maximumValue + this.m_minimum;

			this.SetProgressValue();
		}

		protected void SetProgressValue()
		{
			if (!Taskbar.m_isWin7) return;

			UInt64 min, val, max;

			min = Convert.ToUInt64(this.m_minimum);
			val = Convert.ToUInt64(this.m_value);
			max = Convert.ToUInt64(this.m_maximum);

			this.m_taskbarList.SetProgressValue(this.OwnerHandle, val - min, max - min);
		}

		public void SetProgressState(TaskbarProgressBarState state)
		{
			if (!Taskbar.m_isWin7) return;

			this.m_state = state;

			this.m_taskbarList.SetProgressState(this.OwnerHandle, (WinAPIs.TBPFLAG)state);
		}
		#endregion

		public void SetOverlayIcon(Icon icon, string accessibilityText)
		{
			if (!Taskbar.m_isWin7) return;

			this.m_taskbarList.SetOverlayIcon(this.OwnerHandle, (icon != null ? icon.Handle : IntPtr.Zero), accessibilityText);
		}
	}

	public class WinAPIs
	{
#pragma warning disable 108
		[GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
		[ClassInterfaceAttribute(ClassInterfaceType.None)]
		[ComImportAttribute()]
		public class CTaskbarList { }

		[ComImportAttribute()]
		[GuidAttribute("c43dc798-95d1-4bea-9030-bb99e2983a1a")]
		[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
		public interface ITaskbarList4
		{
			[PreserveSig]
			void HrInit();
			[PreserveSig]
			void AddTab(IntPtr hwnd);
			[PreserveSig]
			void DeleteTab(IntPtr hwnd);
			[PreserveSig]
			void ActivateTab(IntPtr hwnd);
			[PreserveSig]
			void SetActiveAlt(IntPtr hwnd);
			[PreserveSig]
			void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
			[PreserveSig]
			void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
			[PreserveSig]
			void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
			[PreserveSig]
			void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
			[PreserveSig]
			void UnregisterTab(IntPtr hwndTab);
			[PreserveSig]
			void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
			[PreserveSig]
			void SetTabActive(IntPtr hwndTab, IntPtr hwndInsertBefore, uint dwReserved);
			[PreserveSig]
			uint ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButtons);
			[PreserveSig]
			uint ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButtons);
			[PreserveSig]
			void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
			[PreserveSig]
			void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
			[PreserveSig]
			void SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);
			[PreserveSig]
			void SetThumbnailClip(IntPtr hwnd, IntPtr prcClip);
			void SetTabProperties(IntPtr hwndTab, STPFLAG stpFlags);
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct THUMBBUTTON
		{
			[MarshalAs(UnmanagedType.U4)]
			internal THBMASK dwMask;
			internal uint iId;
			internal uint iBitmap;
			internal IntPtr hIcon;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			internal string szTip;
			[MarshalAs(UnmanagedType.U4)]
			internal THBFLAGS dwFlags;
		}

		public enum THBMASK
		{
			THB_BITMAP = 0x1,
			THB_ICON = 0x2,
			THB_TOOLTIP = 0x4,
			THB_FLAGS = 0x8
		}

		public enum TBPFLAG
		{
			TBPF_NOPROGRESS = 0,
			TBPF_INDETERMINATE = 0x1,
			TBPF_NORMAL = 0x2,
			TBPF_ERROR = 0x4,
			TBPF_PAUSED = 0x8
		}

		[Flags]
		public enum THBFLAGS
		{
			THBF_ENABLED = 0x00000000,
			THBF_DISABLED = 0x00000001,
			THBF_DISMISSONCLICK = 0x00000002,
			THBF_NOBACKGROUND = 0x00000004,
			THBF_HIDDEN = 0x00000008,
			THBF_NONINTERACTIVE = 0x00000010
		}

		public enum STPFLAG
		{
			STPF_NONE = 0x0,
			STPF_USEAPPTHUMBNAILALWAYS = 0x1,
			STPF_USEAPPTHUMBNAILWHENACTIVE = 0x2,
			STPF_USEAPPPEEKALWAYS = 0x4,
			STPF_USEAPPPEEKWHENACTIVE = 0x8
		}

#pragma warning restore 108
	}

}
