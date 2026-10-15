using System;
using System.Drawing;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private NotesForm? _notesForm;
	private PlannerForm? _plannerForm;

	private void ShowNotes()
	{
		if (_notesForm != null && !_notesForm.IsDisposed)
		{
			_notesForm.Activate();
			return;
		}
		_notesForm = new NotesForm(Height);
		_notesForm.FormClosed += (_, _) => _notesForm = null;
		_notesForm.Location = new Point(Left - _notesForm.Width, Top);
		_notesForm.Show(this);
	}

	private void ShowPlanner()
	{
		if (_plannerForm != null && !_plannerForm.IsDisposed)
		{
			_plannerForm.Activate();
			return;
		}
		_plannerForm = new PlannerForm(Height);
		_plannerForm.FormClosed += (_, _) => _plannerForm = null;
		_plannerForm.Location = new Point(Left - _plannerForm.Width, Top);
		_plannerForm.Show(this);
	}
}
