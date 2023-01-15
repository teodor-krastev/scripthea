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
# https://realpython.com/python-sockets/#echo-client

import time
import socket
import json

socket_host = socket.gethostname() #"127.0.0.1"
socket_port = 5344 # socket server port number
client_socket = socket.socket()  # instantiate
debugComm = True
def dprint(txt):
    if debugComm:
        print(txt)

def wait4server():
    timeOut = 30 # five min

    while (timeOut > 0):
        try:
            client_socket.connect((socket_host, socket_port))  # connect to the server
            break
        except:
            time.sleep(10)
            dprint('comm attempts left (esc to cancel): '+str(timeOut))
            timeOut -= 1
    return (timeOut > 0)

def OneShot():
    try:
        message = '@next.prompt\n'
        client_socket.send(message.encode())
        dprint('out: '+message)
        inData = client_socket.recv(4096).decode()

        dprint('in: '+inData)
        if (inData.lower().strip() == '@close.session'):
            return inData

        time.sleep(10) # processing

        message = '@image.ready\n'
        client_socket.send(message.encode())
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
    client_socket.close()  # close the connection
    dprint('session closed')

if __name__ == '__main__':
    client_program()
