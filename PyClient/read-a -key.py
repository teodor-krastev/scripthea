#-------------------------------------------------------------------------------
# Name:        module1
# Purpose:
#
# Author:      User
#
# Created:     01/02/2023
# Copyright:   (c) User 2023
# Licence:     <your licence>
#-------------------------------------------------------------------------------
import keyboard

def main():
    keyboard.on_press(lambda key: print(key.name))
    keyboard.wait()
    pass

if __name__ == '__main__':
    main()
