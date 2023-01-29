#-------------------------------------------------------------------------------
# Name:        Test Scripthea client
# Purpose:
#
# Author:      TK
#
# Created:     15/10/2022
# Copyright:   (c) User 2022
# Licence:     <your licence>
#-------------------------------------------------------------------------------
import os
import msvcrt

pipe = -999999 # os.open("\\\\.\\pipe\\ScriptheaPipe", os.O_RDWR)

debugComm = True
def dprint(txt):
    if debugComm:
        print(txt)

def wait4server():
    timeOut = 12 # five min
    while (timeOut > 0):
        try:
            pipe = os.open("\\\\.\\pipe\\ScriptheaPipe", os.O_RDWR) # connect to the server
            break
        except:
            time.sleep(10)
            dprint('comm attempts left: '+str(timeOut))
            timeOut -= 1
    return (timeOut > 0)

def OneShot():
    try:
        message = '@next.prompt\n'
        os.write(pipe, message.encode())
        dprint('out: '+message)
        inData = os.read(pipe, 1024)

        dprint('in: '+inData)
        if (inData.lower().strip() == '@close.session'):
            return inData

        time.sleep(10) # processing

        message = '@image.ready\n'
        os.write(pipe, message.encode())
        time.sleep(1)
    except:
        return '@close.session'
    return inData

def client_program():
    #host = socket.gethostname()  # as both code is running on same pc
    if not wait4server():
        dprint('TIME OUT !')
        return False
    dprint('session started')
    while True:
        jsn_str = OneShot()
        if (jsn_str.lower().strip() == '@close.session'):
            break
        jsn = json.loads(jsn_str)
        dprint('+>'+jsn_str)
        time.sleep(1)
     # close the connection
    dprint('session closed')

if __name__ == '__main__':
    client_program()
