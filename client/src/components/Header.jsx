import React from 'react';
import { Link } from 'react-router-dom';
import { Button } from "@/components/ui/button"

import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"



const Header = () => {

    let name = sessionStorage.getItem('name');

    return (
        <div className="fixed top-0 left-0 right-0 z-50 mx-auto max-w-[1280px] px-4 md:px-6 lg:px-8">
            <header className="flex h-20 w-full shrink-0 items-center px-4 md:px-6">
                <Link href="#" className="mr-6 hidden lg:flex" prefetch={false}>
                    <img src="https://www.pngkey.com/png/detail/372-3728812_logo-autocad-png-pluspng-autodesk-autocad-logo-png.png" className="h-8 w-auto" alt="Vite logo" />
                </Link>
                <div className="ml-auto flex gap-2">
                    <Link
                        className="group inline-flex h-9 w-max items-center justify-center rounded-md bg-white px-4 py-2 text-sm font-medium transition-colors hover:bg-gray-100 hover:text-gray-900 focus:bg-gray-100 focus:text-gray-900 focus:outline-none disabled:pointer-events-none disabled:opacity-50 data-[active]:bg-gray-100/50 data-[state=open]:bg-gray-100/50 dark:bg-gray-950 dark:hover:bg-gray-800 dark:hover:text-gray-50 dark:focus:bg-gray-800 dark:focus:text-gray-50 dark:data-[active]:bg-gray-800/50 dark:data-[state=open]:bg-gray-800/50"
                        prefetch={false} to="/home">Home
                    </Link>
                    
                    <Link
                        href="#"
                        className="group inline-flex h-9 w-max items-center justify-center rounded-md bg-white px-4 py-2 text-sm font-medium transition-colors hover:bg-gray-100 hover:text-gray-900 focus:bg-gray-100 focus:text-gray-900 focus:outline-none disabled:pointer-events-none disabled:opacity-50 data-[active]:bg-gray-100/50 data-[state=open]:bg-gray-100/50 dark:bg-gray-950 dark:hover:bg-gray-800 dark:hover:text-gray-50 dark:focus:bg-gray-800 dark:focus:text-gray-50 dark:data-[active]:bg-gray-800/50 dark:data-[state=open]:bg-gray-800/50"
                        prefetch={false}
                    >
                        Cars
                    </Link>
                    <Link
                        href="#"
                        className="group inline-flex h-9 w-max items-center justify-center rounded-md bg-white px-4 py-2 text-sm font-medium transition-colors hover:bg-gray-100 hover:text-gray-900 focus:bg-gray-100 focus:text-gray-900 focus:outline-none disabled:pointer-events-none disabled:opacity-50 data-[active]:bg-gray-100/50 data-[state=open]:bg-gray-100/50 dark:bg-gray-950 dark:hover:bg-gray-800 dark:hover:text-gray-50 dark:focus:bg-gray-800 dark:focus:text-gray-50 dark:data-[active]:bg-gray-800/50 dark:data-[state=open]:bg-gray-800/50"
                        prefetch={false}
                    >
                        Portfolio
                    </Link>


                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Avatar className="h-9 w-9">
                            {/* <AvatarImage src="/placeholder-user.jpg" alt="@shadcn" /> ou coloca isso com as iniciais */}
                            {/* <AvatarFallback>HV</AvatarFallback> */}
                            <AvatarImage src="https://github.com/shadcn.png" /> {/* ou coloca uma imagem */}
                            <span className="sr-only">Toggle user menu</span>
                            </Avatar>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent className="w-56">
                            <DropdownMenuItem className="font-bold text-lg">{name}</DropdownMenuItem>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem asChild>
                            <Link href="#" className="block w-full text-left" prefetch={false}>
                                Profile
                            </Link>
                            </DropdownMenuItem>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem asChild>
                            <Link to="/logout">Logout</Link>
                            </DropdownMenuItem>
                        </DropdownMenuContent>
                    </DropdownMenu>

                    {/* <Button variant="outline" className="justify-self-end px-2 py-1 text-xs">Criar conta</Button> */}
                    {/* <Button className="justify-self-end px-2 py-1 text-xs">Login</Button> */}

                    
                </div>
            </header>
        </div>
    )

};

export default Header;
