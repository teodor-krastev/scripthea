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

"""
from xml.dom import minidom
from suds.client import Client

def main():
    url = "http://localhost:8000/sample"
    client = Client(url + "?wsdl")

    name = "John"
    result = client.service.SayHello(name)
    print(result)
"""
import os
import msvcrt

def main():
    pipe = os.open("\\\\.\\pipe\\mypipe", os.O_RDWR)

    while True:
        message = input("Enter a message: ")+"\n"
        os.write(pipe, message.encode())

        if message == "exit":
            break

        response = os.read(pipe, 1024)
        print("Received:", response.decode())

    os.close(pipe)

if __name__ == '__main__':
    main()
