# Inventory Notes Genorator

A simple application that will parse a block of text and format the contents into two tables. The first table is for items, the second is the currency split amongst the list of players.

## Output Textbox

The output window shows the markdown formmated results parsed from the **Players** and **Items/Currenct** textboxes.
The output is updated live and is formatted to support the limited markdown parsing supported by Discord.
Copy the output textbox by selecting all the text, including the backtick (`) marks at the start and end of the textblock.


## Players Textbox

List the players that are to receive an equal portion of the currency.

Separate players with a space or on its own line.

the **last** player marked with an **(*)** either at the beginning or end of the players name will be set to be the first player to receive the distributed currency. 
This is to let the user track which players were shorted the last time currency was handed out and make sure that player is distributed to first.

#### Example

(Players)
``` 
Chasz Chass Shadow
Skaggic *Niles Uunsha Elmo(N)
```

(Items/Currency)
``` Items/Currency
13g
```

#### Result
```
# Money (0p 13g 0e 0s 0c)
| Player  | Currency       | Equivalent |
| ------- | -------------- | ---------- |
| Chasz   | 0p 2g 0e 0s 0c | 2g 0s 0c   |
| Chass   | 0p 2g 0e 0s 0c | 2g 0s 0c   |
| Shadow  | 0p 2g 0e 0s 0c | 2g 0s 0c   |
| Skaggic | 0p 1g 0e 0s 0c | 1g 0s 0c   | (Rounding)
| Niles   | 0p 2g 0e 0s 0c | 2g 0s 0c   |
| Uunsha  | 0p 2g 0e 0s 0c | 2g 0s 0c   |
| Elmo(N) | 0p 2g 0e 0s 0c | 2g 0s 0c   |
```

## Items/Currency Textbox

This textbox will try to parse the user input line by line to find the following

### Title

the last line that starts with a **#** will have the text following the **#** as the title for the output text

#### Exmple
```
# 1234
#abcd
```

#### Result
```
# abcd
....
```

### Items

To add an item to the items output table it must follow the following pattern. Each item must be on a line by itself

(number of items) (name of items, spaces allowed) @(holder)

#### Example
```
7 Arrows of Walloping @John
```

#### Result
``` 
| 7      | Arrows of Walloping | John |
```

### Currency

Each line that does parse to be the title or an item may contain any combinaion of currency, including multiple of the same currency.
Currency must follow the (amount)(type) format.

The amount must be a positive whole number, the type can be:

p = platinum

g = gold

e = electrum

s = silver

c = copper

#### Example
```
14p 21g 5c 315g 92s
82s 15c
```

#### Result
```
# Money ( 14p 336g 0e 174s 20c )
...
```