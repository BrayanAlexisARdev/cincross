using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private LearningData _learningData = LearningData.CreateDefault();

	private bool _isLearningSession;

	private string _currentSubject = "";

	private void btnLearn_Click(object? sender, EventArgs e)
	{
		btnLearn.Enabled = false;
		btnAddFav.Enabled = false;

		var form = new LearnForm(_learningData, 0);
		form.Location = new Point(Left - form.Width, Top);

		form.FormClosed += (_, _) =>
		{
			btnLearn.Enabled = true;
			btnAddFav.Enabled = true;
			form.Dispose();
		};

		form.Show(this);
	}

	private void CompleteLearningSession()
	{
		if (!_isLearningSession) return;
		_isLearningSession = false;

		SubjectData? subj = _learningData.Subjects.Find(s => s.Name == _currentSubject);
		if (subj == null) return;

		int prevLevel = subj.Level;
		int prevStreak = subj.CurrentStreak;
		int minutes = (int)(_timeRemaining.TotalMinutes > 0 ? _timeRemaining.TotalMinutes : 0);

		int sessionMinutes = Math.Max(1, minutes);
		subj.CompleteSession(sessionMinutes);

		_learningData.Save();

		if (subj.CurrentStreak > prevStreak)
		{
			_ = FlashMessage($"🔥 ¡Racha de {subj.CurrentStreak} días!", Color.FromArgb(255, 150, 0));
		}

		if (subj.Level > prevLevel)
		{
			_ = FlashMessage($"🎉 ¡SUBISTE AL NIVEL {subj.Level} en {subj.DisplayName}!", Color.FromArgb(88, 204, 2));
		}

		if (subj.TodayMinutesStudied >= _learningData.DailyGoalMinutes)
		{
			_ = FlashMessage($"🎯 ¡META DIARIA COMPLETADA!", Color.FromArgb(88, 204, 2));
		}
	}

	private async Task FlashMessage(string message, Color color)
	{
		string origText = lblMetadata.Text;
		Color origColor = lblMetadata.ForeColor;
		lblMetadata.Text = message;
		lblMetadata.ForeColor = color;
		await Task.Delay(3000);
		lblMetadata.Text = origText;
		lblMetadata.ForeColor = origColor;
	}
}
