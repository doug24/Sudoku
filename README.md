# Sudoku
A free Sudoku game with both classic and irregular layouts

There are two input modes

- When the "1234" button is pressed (highlighted), select a number from the digit pad then click a cell in the game board to place the number. Left click places an answer, and right click places a pencil mark. Click again to remove the answer or pencil mark. If the pencil button is selected, the left and right mouse buttons are reversed. The numbers on the keyboard can also be used to select digits. 
- When the "1234" button is not pressed, first select one or more cells in the game board, then click a digit button or keyboard number to place a digit. Click the pen or pencil button to change between answers and pencil marks. Use ctrl + click to select multiple cells. Double click a number on the game board to highlight the selected number when that option is enabled.

The digit pad also has **colors** for color solution techniques.
The last row has buttons for **Undo**, **Redo**, and **Clear all colors**


### Keyboard shortcuts:

| Key | Command |
| --- | --- |
| 1 - 9 | select or place a digit |
| a | select Pen (answer) |
| q | select Pencil |
| ctrl + s | snapshot the game in progress |
| ctrl + z | undo |
| ctrl + y | redo |
| delete | clear colors |


### Options

- **Color incorrect answers** - save yourself from taking a wrong turn.
- **Clean pencil marks** - will remove pencil marks for you when you place an answer.
- **Highlight selected number** - circles answers and shows pencil marks in red.
- **Timer** - show the solve time. The timer will pause when the game is minimized.
- **Fill candidates** - puts all possible candidates on the board, does not eliminate any by logic.
- **Fast forward** - fills the answer in a cell when there is only one candidate remaining. Combined with *Clean pencil marks* it can speed through the end of the game.

Sorry, there are no suggestions if you get stuck.

### Game generator

The game generator is based on QQWing. QQWing was ported to c# and them modified to work with irregular board layouts.

Under the **File** menu, choose a layout, symmetry, and difficulty. 

Click **File - Create New** to generate a new game on the current board layout.

Click **File - Create New Random** to generate a new game on a randomly selected board layout.

The game generator may have a hard time getting a new game based on the layout, symmetry, and difficulty options, and will give up after 20 seconds. Just try again, or change the layout, symmetry, or difficulty options.

You can **Snapshot** and then **Restore** a game in progress.

You can load or save games in the Simple Sudoku (*.ss) file format.

The **Edit** menu allows you to manually input a Sudoku game from a book or newspaper.

