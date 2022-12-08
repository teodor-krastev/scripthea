#-------------------------------------------------------------------------------
# Name:        module1
# Purpose:
#
# Author:      User
#
# Created:     15/10/2022
# Copyright:   (c) User 2022
# Licence:     <your licence>
#-------------------------------------------------------------------------------

import time
import socket
import json

comm_port = 5344 # socket server port number
client_socket = socket.socket()  # instantiate
debugComm = True
def dprint(txt):
    if debugComm:
        print(txt)

def wait4server():
    host = socket.gethostname()
    timeOut = 30 # five min
    while (timeOut > 0):
        try:
            client_socket.connect((host, comm_port))  # connect to the server
            break
        except:
            time.sleep(10)
            dprint('comm attempts left: '+str(timeOut))
            timeOut -= 1
    return (timeOut > 0)

def client_program():
    host = socket.gethostname()  # as both code is running on same pc
    if not wait4server():
        dprint('TIME OUT !')
        return False
    message = '@open.session' # input(" -> ")  # take input
    client_socket.send(message.encode())  # start message
    dprint('session started')
    while True:
        inData = client_socket.recv(4096).decode()
        dprint('+>'+inData)
        if (inData.lower().strip() == '@close.session'):
            break
        #jsn = json.loads(inData)
        #dprint(jsn["prompt"])
        time.sleep(5)
        #jsn["filename"] = jsn["filename"].upper()
        #message = json.dumps(jsn)
        message = '@next.prompt'
        client_socket.send(message.encode())  # send message to

        # message = input(" -> ")  # again take input
    client_socket.close()  # close the connection
    dprint('session closed')


if __name__ == '__main__':
    client_program()
