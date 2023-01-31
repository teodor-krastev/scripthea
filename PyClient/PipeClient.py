#-------------------------------------------------------------------------------
# Name:        module1
# Purpose:
#
# Author:      User
#
# Created:     25/01/2023
# Copyright:   (c) User 2023
# Licence:     <your licence>
#-------------------------------------------------------------------------------

import os
import msvcrt
import time

debugComm = True
def dprint(txt):
    if debugComm:
        print(txt)

named_pipe2s = "\\\\.\\pipe\\scripthea_pipe2s"
named_pipe2c = "\\\\.\\pipe\\scripthea_pipe2c"

def isPipeOpen(named_pipe):
    return os.path.exists(named_pipe) and os.access(named_pipe, os.R_OK | os.W_OK)

def wait4server():
    timeOut = 12 # five min
    global pipe2s
    global pipe2c
    while (timeOut > 0):
        try:
            pipe2s = os.open(named_pipe2s, os.O_WRONLY)
            pipe2c = os.open(named_pipe2c, os.O_RDONLY)
            break
        except:
            time.sleep(10)
            dprint('comm attempts left: '+str(timeOut))
            timeOut -= 1
    return (timeOut > 0)

def OneShot():
    try:
        message = '@next.prompt\n'
        os.write(pipe2s, message.encode())
        #dprint('out: '+message)
        inData = os.read(pipe2c, 4096).decode().strip()
        #dprint('in: '+inData)
        if inData == "@close.session":
            return inData

        time.sleep(5) # processing

        message = '@image.ready\n'
        os.write(pipe2s, message.encode())
        time.sleep(1)
    except:
        return '@close.session'
    return inData

def main():
    if wait4server() == 0:
        print("TIME OUT to connect to Scripthea pipe-server")
        return
    dprint("open.session")

    while True:
        inData = OneShot()
        dprint('in: '+inData)
        if inData == "@close.session":
            break

    if isPipeOpen(named_pipe2s):
        os.close(pipe2s)
    if isPipeOpen(named_pipe2c):
        os.close(pipe2c)

if __name__ == '__main__':
    main()
