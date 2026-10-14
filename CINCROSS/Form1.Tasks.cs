using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private Panel? _activeTaskPanel;

	private double _activeTaskTotalSeconds;

	private void btnAddTask_Click(object? sender, EventArgs e)
	{
		using EditTaskForm newTaskForm = new EditTaskForm("NEW TASK", "", TimeSpan.Zero, "tasks_TSK.png");
		newTaskForm.Location = new Point(Left - newTaskForm.Width, Top);
		if (newTaskForm.ShowDialog(this) == DialogResult.OK)
		{
			AddTaskToPanel(newTaskForm.TaskName, newTaskForm.TaskTime, "", newTaskForm.SelectedIcon);
		}
	}

	private void AddTaskToPanel(string taskName, TimeSpan taskTime, string m3uPath, string iconName = "tasks_TSK.png", bool isFixed = false)
	{
		(Color, Color, Color, Color) theme = _themes[_currentThemeIndex];
		Panel taskPanel = new Panel
		{
			Height = 60,
			Width = base.Width / 2,
			BackColor = (isFixed ? Color.FromArgb(216, theme.Item2) : theme.Item2),
			Margin = new Padding(0),
			BorderStyle = BorderStyle.None,
			Tag = new TaskData
			{
				Time = taskTime,
				M3uPath = m3uPath,
				IsFixed = isFixed
			}
		};
		PictureBox picTask = new PictureBox
		{
			Size = new Size(32, 32),
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = Color.Transparent,
			Location = new Point(5, 12)
		};
		try
		{
		string testPath = PathHelper.ResolveImg(iconName);
		if (File.Exists(testPath))
			picTask.Image = PathHelper.LoadImage(testPath);
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.AddTaskToPanel.Icon", ex);
		}
		Label lblTaskName = new BufferedLabel
		{
			Text = taskName.ToUpper(),
			Location = new Point(42, 8),
			AutoSize = false,
			Size = new Size(taskPanel.Width - 45, 20),
			BackColor = Color.Transparent,
			Font = ((FontHelper.CustomFontFamily != null) ? new Font(FontHelper.CustomFontFamily, 6.5f, FontStyle.Bold) : new Font("Segoe UI", 6.5f, FontStyle.Bold))
		};
		Label lblTaskTime = new BufferedLabel
		{
			Text = FormatTaskTime(taskTime),
			Location = new Point(42, 28),
			AutoSize = true,
			BackColor = Color.Transparent,
			Font = ((FontHelper.CustomFontFamily != null) ? new Font(FontHelper.CustomFontFamily, 7f, FontStyle.Regular) : new Font("Segoe UI", 7f, FontStyle.Regular))
		};

		taskPanel.Click += delegate
		{
			ToggleTask(taskPanel, (TaskData)taskPanel.Tag);
		};
		lblTaskName.Click += delegate
		{
			ToggleTask(taskPanel, (TaskData)taskPanel.Tag);
		};
		lblTaskTime.Click += delegate
		{
			ToggleTask(taskPanel, (TaskData)taskPanel.Tag);
		};
		picTask.Click += delegate
		{
			ToggleTask(taskPanel, (TaskData)taskPanel.Tag);
		};
		ContextMenuStrip menu = new ContextMenuStrip();
		ToolStripMenuItem itemEdit = new ToolStripMenuItem("EDITAR");
		itemEdit.Click += delegate
		{
			TaskData data = (TaskData)taskPanel.Tag;
			using EditTaskForm editForm = new EditTaskForm("EDIT TASK", lblTaskName.Text, data.Time, "tasks_TSK.png");
			editForm.Location = new Point(Left - editForm.Width, Top);
			if (editForm.ShowDialog(this) == DialogResult.OK)
			{
				lblTaskName.Text = editForm.TaskName.Substring(0, Math.Min(8, editForm.TaskName.Length)).ToUpper();
				data.Time = editForm.TaskTime;
				lblTaskTime.Text = FormatTaskTime(editForm.TaskTime);
				try
				{
					string iconPath = PathHelper.ResolveImg(editForm.SelectedIcon);
					if (File.Exists(iconPath)) picTask.Image = PathHelper.LoadImage(iconPath);
				}
				catch (Exception ex) { Logger.Error("Form1.EditTaskIcon", ex); }
			}
		};
		menu.Items.Add(itemEdit);
		taskPanel.ContextMenuStrip = menu;
		lblTaskName.ContextMenuStrip = menu;
		lblTaskTime.ContextMenuStrip = menu;
		picTask.ContextMenuStrip = menu;
		taskPanel.Controls.Add(lblTaskName);
		taskPanel.Controls.Add(lblTaskTime);
		taskPanel.Controls.Add(picTask);

		tasksListPanel.Controls.Add(taskPanel);
		UpdateControlContrast(taskPanel, theme.Item2);
	}

	private void ToggleTask(Panel taskPanel, TaskData data)
	{
		if (_activeTaskPanel == taskPanel)
		{
			StopTimer();
			_activeTaskPanel = null;
			_timeRemaining = TimeSpan.Zero;
			UpdateTimerDisplay();
			ResetTaskProgress(taskPanel);
		}
		else if (!_timerRunning)
		{
			_activeTaskPanel = taskPanel;
			_activeTaskTotalSeconds = data.Time.TotalSeconds;
			_timeRemaining = data.Time;
			UpdateTimerDisplay();
			StartTimer();
		}
		UpdateTaskInfo();
	}

	private void UpdateTaskInfo()
	{
		if (_activeTaskPanel != null)
		{
			Label? nameLabel = null;
			foreach (Control c in _activeTaskPanel.Controls)
			{
				if (c is Label lbl)
				{
					nameLabel = lbl;
					break;
				}
			}
			if (nameLabel != null && _activeTaskPanel.Tag is TaskData data)
			{
				SetLabelState(lblTaskInfo, FormatTaskTime(data.Time), Color.White);
				SetLabelState(lblTaskRemaining, FormatTaskTime(_timeRemaining), Color.Gray);
			}
		}
		else if (_timeRemaining.TotalSeconds > 0)
		{
			double total = _manualTotalSeconds > 0 ? _manualTotalSeconds : _timeRemaining.TotalSeconds;
			SetLabelState(lblTaskInfo, FormatTaskTime(TimeSpan.FromSeconds(total)), Color.White);
			SetLabelState(lblTaskRemaining, FormatTaskTime(_timeRemaining), Color.Gray);
		}
		else
		{
			SetLabelState(lblTaskInfo, "---", Color.Gray);
			SetLabelState(lblTaskRemaining, "", Color.Gray);
		}
	}

	private void UpdateTaskProgress()
	{
		if (_nodeIntensities.Length == 0) return;
		double progress = _activeTaskPanel == null ? 0
			: (_activeTaskTotalSeconds - _timeRemaining.TotalSeconds) / _activeTaskTotalSeconds;

		for (int i = 0; i < _nodeCount; i++)
		{
			double nodeStart = (double)i / _nodeCount;
			double nodeEnd = (double)(i + 1) / _nodeCount;
			double raw = 0;

			if (progress >= nodeEnd)
				raw = 1;
			else if (progress > nodeStart)
				raw = (progress - nodeStart) / (nodeEnd - nodeStart);

			double eased = raw < 0.5 ? 4 * raw * raw * raw : 1 - Math.Pow(-2 * raw + 2, 3) / 2;
			_nodeIntensities[i] = Math.Min(eased * 1.2, 1);
		}
		pnlProgressFill.Invalidate();
	}

	private void ResetTaskProgress(Panel taskPanel)
	{
		if (_nodeIntensities.Length == 0) return;
		for (int i = 0; i < _nodeCount; i++)
			_nodeIntensities[i] = 0;
		pnlProgressFill.Invalidate();
	}

	private static string FormatTaskTime(TimeSpan t)
	{
		if (t.TotalHours >= 1.0)
			return $"{(int)t.TotalHours}H {t.Minutes}M";
		return $"{(int)t.TotalMinutes} MIN";
	}
}
