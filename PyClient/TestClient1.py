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


def client_program():
    host = socket.gethostname()  # as both code is running on same pc
    port = 5344 # socket server port number

    client_socket = socket.socket()  # instantiate
    client_socket.connect((host, port))  # connect to the server

    message = 'start' #input(" -> ")  # take input
    client_socket.send(message.encode())  # start message
    print('session started')
    while True:
        inData = client_socket.recv(4096).decode()
        if (inData.lower().strip() == 'end'):
            break
        jsn = json.loads(inData)
        print(jsn["dog"])
        time.sleep(5)
        jsn["cat"] = jsn["cat"].upper()
        message = json.dumps(jsn)
        client_socket.send(message.encode())  # send message

        # message = input(" -> ")  # again take input
    client_socket.close()  # close the connection
    print('session closed')


if __name__ == '__main__':
    client_program()
