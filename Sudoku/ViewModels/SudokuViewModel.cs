﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using QQWingLib;

namespace Sudoku;

public partial class SudokuViewModel : ObservableObject
{
    private readonly static Random rand = new();

    public SudokuViewModel()
    {
        SectionLayout = Properties.Settings.Default.SectionLayout;

        if (Enum.TryParse(Properties.Settings.Default.PuzzleDifficulty, out Difficulty diff))
        {
            PuzzleDifficulty = diff;
        }
        if (Enum.TryParse(Properties.Settings.Default.PuzzleSymmetry, out Symmetry symm))
        {
            PuzzleSymmetry = symm;
        }

        IrregularLayout layout = new();
        LayoutMenuItems.Add(new("Classic", -1, SectionLayout == -1));
        LayoutMenuItems.Add(new("Random", 999, SectionLayout == 999));
        for (int idx = 0; idx < IrregularLayout.LayoutCount; idx++)
        {
            LayoutMenuItems.Add(new($"Irregular {idx + 1}", idx, SectionLayout == idx));
        }

        WeakReferenceMessenger.Default.Register<SectionLayoutChangedMessage>(this, (r, m) =>
        {
            int index = m.Value;
            if (m.Value == 999)
            {
                index = rand.Next(0, IrregularLayout.LayoutCount);
            }

            SectionLayout = index;
            LayoutMenuItems.ForEach(menu => menu.IsChecked = menu.LayoutId == index);
        });
    }


    internal void SaveSettings()
    {
        Properties.Settings.Default.SectionLayout = SectionLayout;
        Properties.Settings.Default.PuzzleDifficulty = PuzzleDifficulty.ToString();
        Properties.Settings.Default.PuzzleSymmetry = PuzzleSymmetry.ToString();

        GameBoard.SaveSettings();

        Properties.Settings.Default.Save();
    }

    [ObservableProperty]
    private GameBoardViewModel gameBoard = new();

    [ObservableProperty]
    private int sectionLayout = QQWing.ClassicLayout;

    partial void OnSectionLayoutChanged(int value)
    {
        GameBoard.ChangeLayout(value);
    }

    [ObservableProperty]
    private Symmetry puzzleSymmetry = Symmetry.MIRROR;

    [ObservableProperty]
    private Difficulty puzzleDifficulty = Difficulty.INTERMEDIATE;

    [ObservableProperty]
    private bool isHighlightMode;

    public ObservableCollection<LayoutMenuItemViewModel> LayoutMenuItems { get; set; } = [];


    public ICommand NewPuzzleCommand => new RelayCommand(
        p => GameBoard.NewPuzzle(PuzzleDifficulty, PuzzleSymmetry));

    public ICommand NewRandomPuzzleCommand => new RelayCommand(
        p =>
        {
            int index = rand.Next(0, IrregularLayout.LayoutCount);
            WeakReferenceMessenger.Default.Send(new SectionLayoutChangedMessage(index));
            GameBoard.NewPuzzle(PuzzleDifficulty, PuzzleSymmetry);
        });

    public ICommand SnapshotCommand => new RelayCommand(
        p => Snapshot(),
        q => GameBoard.IsInProgress);

    public ICommand EnterDesignModeCommand => new RelayCommand(
        p => GameBoard.EnterDesignMode(),
        q => !GameBoard.IsDesignMode);

    public ICommand ExitDesignModeCommand => new RelayCommand(
        p => GameBoard.ExitDesignMode(),
        q => GameBoard.IsDesignMode);

    public ICommand ClearBoardCommand => new RelayCommand(
        p => GameBoard.ClearBoard(),
        q => GameBoard.IsDesignMode);

    public ICommand NumberKeyCommand => new RelayCommand(
        p => NumberKey(p));

    public ICommand ColorKeyCommand => new RelayCommand(
        p => ColorKey(p));

    public ICommand OpenFileCommand => new RelayCommand(
        p => OpenFile());

    public ICommand SaveAsCommand => new RelayCommand(
        p => SaveAs());

    public ICommand RestoreCommand => new RelayCommand(
        p => Restore(),
        q => HasSessionFile());

    public ICommand UndoCommand => new RelayCommand(
        p => GameBoard.Undo(),
        q => GameBoard.CanUndo);

    public ICommand RedoCommand => new RelayCommand(
        p => GameBoard.Redo(),
        q => GameBoard.CanRedo);

    public ICommand ClearCommand => new RelayCommand(
        p => GameBoard.ClearColors());

    private void NumberKey(object p)
    {
        if (p is string num && int.TryParse(num, out int value))
        {
            if (GameBoard.NumberFirstMode)
            {
                GameBoard.SetSelectedNumber(value);
            }
            else
            {
                GameBoard.SelectedNumber = NumberSelection.None;

                if (IsHighlightMode)
                {
                    GameBoard.HighlightNumbers(value);
                    IsHighlightMode = false;
                    return;
                }

                GameBoard.KeyDown(value);
            }
        }
    }

    private void ColorKey(object p)
    {
        if (p is string num && int.TryParse(num, out int value))
        {
            if (GameBoard.NumberFirstMode)
            {
                GameBoard.SetSelectedNumber(value);
            }
            else
            {
                IsHighlightMode = false;
                GameBoard.SetColor(value - 10);
            }
        }
    }

    internal void KeyDown(KeyEventArgs e)
    {
        bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (IsHighlightMode)
        {
            int number = KeyToNumber(key);
            GameBoard.HighlightNumbers(number);
            IsHighlightMode = false;
            return;
        }

        if (!GameBoard.NumberFirstMode)
        {
            GameBoard.SelectedNumber = NumberSelection.None;
        }

        switch (key)
        {
            case Key.D1:
            case Key.NumPad1:
                if (GameBoard.NumberFirstMode)
                    GameBoard.SetSelectedNumber(1);
                else
                    GameBoard.KeyDown(1);
                break;
            case Key.D2:
            case Key.NumPad2:
                if (GameBoard.NumberFirstMode)
                    GameBoard.SetSelectedNumber(2);
                else
                    GameBoard.KeyDown(2);
                break;
            case Key.D3:
            case Key.NumPad3:
                if (GameBoard.NumberFirstMode)
                    GameBoard.SetSelectedNumber(3);
                else
                    GameBoard.KeyDown(3);
                break;
            case Key.D4:
            case Key.NumPad4:
                if (GameBoard.NumberFirstMode)
                    GameBoard.SetSelectedNumber(4);
                else
                    GameBoard.KeyDown(4);
                break;
            case Key.D5:
            case Key.NumPad5:
                if (GameBoard.NumberFirstMode)
                    GameBoard.SetSelectedNumber(5);
                else
                    GameBoard.KeyDown(5);
                break;
            case Key.D6:
            case Key.NumPad6:
                if (GameBoard.NumberFirstMode)
                    GameBoard.SetSelectedNumber(6);
                else
                    GameBoard.KeyDown(6);
                break;
            case Key.D7:
            case Key.NumPad7:
                if (GameBoard.NumberFirstMode)
                    GameBoard.SetSelectedNumber(7);
                else
                    GameBoard.KeyDown(7);
                break;
            case Key.D8:
            case Key.NumPad8:
                if (GameBoard.NumberFirstMode)
                    GameBoard.SetSelectedNumber(8);
                else
                    GameBoard.KeyDown(8);
                break;
            case Key.D9:
            case Key.NumPad9:
                if (GameBoard.NumberFirstMode)
                    GameBoard.SetSelectedNumber(9);
                else
                    GameBoard.KeyDown(9);
                break;
            case Key.S:
                if (ctrl && GameBoard.IsInProgress)
                    Snapshot();
                break;
            case Key.Y:
                if (ctrl)
                    GameBoard.Redo();
                break;
            case Key.Z:
                if (ctrl)
                    GameBoard.Undo();
                break;
            case Key.Delete:
                GameBoard.ClearColors();
                break;
            case Key.Q:
                GameBoard.KeyInputMode = KeyPadMode.Pencil;
                break;
            case Key.A:
                GameBoard.KeyInputMode = KeyPadMode.Pen;
                break;
        }
        e.Handled = true;
    }

    private static int KeyToNumber(Key key)
    {
        return key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            Key.D5 or Key.NumPad5 => 5,
            Key.D6 or Key.NumPad6 => 6,
            Key.D7 or Key.NumPad7 => 7,
            Key.D8 or Key.NumPad8 => 8,
            Key.D9 or Key.NumPad9 => 9,
            _ => -1,
        };
    }

    private void OpenFile()
    {
        OpenFileDialog dlg = new()
        {
            DefaultExt = ".ss",
            Filter = "Simple Sudoku Files|*.ss"
        };
        var result = dlg.ShowDialog();
        if (result == true)
        {
            string[] lines = File.ReadAllLines(dlg.FileName);
            GameBoard.OpenSimpleSudoku(lines);
        }
    }

    private void SaveAs()
    {
        SaveFileDialog dlg = new()
        {
            DefaultExt = ".ss",
            Filter = "Simple Sudoku Files|*.ss"
        };
        var result = dlg.ShowDialog();
        if (result == true)
        {
            string puzzle = GameBoard.ToSimpleSudokuString();
            File.WriteAllText(dlg.FileName, puzzle);
        }
    }

    private void Snapshot()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var file = Path.Combine(path, "session.sudoku");
        string state = GameBoard.ToSnapshotString();
        File.WriteAllText(file, state);
    }

    private void Restore()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var file = Path.Combine(path, "session.sudoku");
        string[] ssData = File.ReadAllLines(file);
        GameBoard.Restore(ssData);
    }

    private static bool HasSessionFile()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return File.Exists(Path.Combine(path, "session.sudoku"));
    }
}
