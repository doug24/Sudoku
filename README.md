# Sudoku
A free Sudoku game with classic, 28 different irregular (jigsaw) layouts, even/odd and killer puzzle modes. The game supports pencil marks, color highlighting, undo/redo, and a game generator with multiple difficulty levels.

| Classic | Irregular |
|---|---|
|<img src="https://github.com/doug24/Sudoku/assets/17227248/e9aa99be-b184-4160-97fb-2630ca760699"/>|<img src="https://github.com/doug24/Sudoku/assets/17227248/03351dc3-f165-4475-901f-0f2336c05b05"/>|

### Input Modes

There are two input modes:

- **Number first** - When the "1234" button is pressed (highlighted), select a number from the digit pad then click a cell in the game board to place the number. Left click places an answer, and right click places a pencil mark. Click again to remove the answer or pencil mark. If the pencil button is selected, the left and right mouse buttons are reversed. The numbers on the keyboard can also be used to select digits.
- **Cell first** - When the "1234" button is not pressed, first select one or more cells in the game board, then click a digit button or keyboard number to place a digit in the selected cells. Click the pen or pencil button to change between answers and pencil marks. Use Ctrl+click to select multiple cells. Double-click a number on the game board to highlight all instances of that number when that option is enabled.

The digit pad also has **colors** for color solution techniques.
The last row has buttons for **Undo**, **Redo**, and **Clear all colors**.


### Keyboard Shortcuts

| Key | Command |
| --- | --- |
| 1 – 9 | Select or place a digit |
| a | Select Pen (answer) |
| q | Select Pencil |
| Ctrl+S | Snapshot the game in progress |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Delete | Clear colors |


### Options

- **Color incorrect answers** - Highlights wrong answers so you can avoid taking a wrong turn.
- **Clean pencil marks** - Automatically removes pencil marks when you place an answer.
- **Highlight selected number** - Circles answers and shows pencil marks in red for the selected digit.
- **Timer** - Displays your solve time. The timer pauses when the game is minimized.
- **Fill candidates** - Places all possible candidates on the board without eliminating any by logic.
- **Fast forward** - Automatically fills in a cell when only one candidate remains. Combined with *Clean pencil marks*, this can speed through the end of the game.


### Hints

Hints may be available once candidates are placed on the board. When a hint is available, it highlights the relevant cells and identifies the solving strategy being applied, helping you understand the next logical step without giving away the answer outright.


### Game Generator

The game generator is based on QQWing, which was ported to C# and modified to support irregular board layouts.

Under the **File** menu, choose a layout, symmetry, and difficulty.

Click **File - Create New** to generate a new game using the current settings.

Click **File - Create New Random** to generate a new game on a randomly selected board layout.

Difficulty levels include **Easy**, **Intermediate**, **Tough**, and **Expert**. Tough puzzles require advanced solver strategies. Expert are the most challenging to complete.

The generator will try to produce a puzzle matching your settings. If it hasn't succeeded after 10 seconds, you can let it run longer or cancel. Try again after adjusting the layout, symmetry, or difficulty.

You can **Snapshot** and then **Restore** a game in progress.

You can load or save games in the Simple Sudoku (*.ss) file format. The **Import** menu allows you to load a Sudoku puzzle from a variety of different formats.

The **Edit** menu allows you to manually enter a classic Sudoku puzzle from a book or newspaper.


### Puzzle Modes

#### Even/Odd Sudoku

Even/Odd Sudoku adds a constraint on top of classic Sudoku rules. Certain cells are highlighted to indicate that their values must be either all even or all odd. An indicator cell in the puzzle shows whether the highlighted cells are even or odd.

#### Killer Sudoku

Killer Sudoku follows classic Sudoku rules with an added twist: the board is divided into groups of cells called **cages**. Each cage displays a target sum in its corner, and the digits placed within the cage must add up to exactly that total.

**Generator:** Killer puzzles use your current symmetry and difficulty settings.

> **Note:** 90° rotational symmetry is not supported for Killer puzzles and will automatically default to another symmetric pattern.

**Visual layout:** Cages are displayed in distinct colors for easy visibility. Because Killer Sudoku relies on color, it is only available for the 9×9 board layout.

**Hints & Tools:**

- **Killer Calculator** - An optional tool to help you eliminate candidate values from cages based on the cage sum and size.
- **Fill Candidates** - The Fill Candidates button follows standard Sudoku rules and does not account for cage sums.
- **Hints** - Hints based on standard Sudoku strategies may be available once candidates are placed on the board.


### Themes

The application supports both **light** and **dark** themes. Switch between them to suit your preference or lighting conditions. The selected theme is saved between sessions.

