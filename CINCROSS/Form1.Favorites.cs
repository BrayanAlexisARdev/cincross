using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private List<FavoriteData> _favorites = new List<FavoriteData>();

	private void btnAddFav_Click(object? sender, EventArgs e)
	{
		if (_currentCassetteIndex < 0 || _currentCassetteIndex >= _cassettes.Count) return;
		CassetteData cass = _cassettes[_currentCassetteIndex];
		string title = cass.Titulo;
		if (string.IsNullOrWhiteSpace(title)) title = $"Cassette {_currentCassetteIndex + 1}";

		_favorites.RemoveAll(f => f.CassetteIndex == _currentCassetteIndex);
		_favorites.Add(new FavoriteData
		{
			CassetteIndex = _currentCassetteIndex,
			CassetteTitle = title,
			ColorHex = _currentColorHex,
			TemaTV = _currentTvPath
		});
	}

	private void btnInfo_Click(object? sender, EventArgs e)
	{
		using var form = new InfoForm();
		form.Location = new Point(Left - form.Width, Top);
		form.ShowDialog(this);
	}

	private void btnFavList_Click(object? sender, EventArgs e)
	{
		using var form = new FavoritesListForm(_favorites.ToArray());
		form.Location = new Point(Left - form.Width, Top);
		if (form.ShowDialog(this) == DialogResult.OK && form.SelectedIndex >= 0)
		{
			var fav = _favorites[form.SelectedIndex];
			ApplyFavorite(fav);
		}
	}

	private void ApplyFavorite(FavoriteData fav)
	{
		if (fav.CassetteIndex == _currentCassetteIndex)
			_currentCassetteIndex = -1;
		GoToCassette(fav.CassetteIndex);
		ApplyFavOverride(fav);
	}

	private void ApplyFavOverride(FavoriteData fav)
	{
		if (!string.IsNullOrEmpty(fav.ColorHex))
		{
			try { ApplyAppColor(ColorTranslator.FromHtml(fav.ColorHex)); } catch (Exception ex) { Logger.Error("Form1.ApplyFavOverride.Color", ex); }
		}
		if (!string.IsNullOrEmpty(fav.TemaTV))
		{
			Image? tvImg = GetCachedImage(fav.TemaTV);
			if (tvImg != null)
			{
				SetTimerBackground(tvImg, true);
				timerPanel.Height = tvImg.Height - 4;
			}
		}
	}
}
