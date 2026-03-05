import React, { useEffect, useState } from 'react';

import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { AlertCircle } from "lucide-react"



import axiosInstance from "@/utils/axiosConfig";



const Login = () => {

    const [erro, setErro] = useState(false);

    useEffect(() => {
        const token = sessionStorage.getItem('token');
        if (token) {
            window.location.href = '/Home';
            return;
        }
    }, []);

    const handleLogin = async (event) => {
        event.preventDefault();
        setErro(false);
        const formData = new FormData(event.target);
        const email = formData.get('email');
        const password = formData.get('password');
        
        axiosInstance
            .post("/login", { email, password })
            .then((response) => {
                const token = response.data;
                const payload = JSON.parse(atob(token.split('.')[1]));
                sessionStorage.setItem('token', token);
                sessionStorage.setItem('name', payload.Name);
                sessionStorage.setItem('email', payload.Email);
                window.location.href = '/home';
            })
            .catch((error) => {
                setErro(true);
            });
    };

    return (
        <form onSubmit={handleLogin}>
            
            <Card className="mx-auto max-w-md">
                <CardHeader className="space-y-1">
                    <CardTitle className="text-2xl font-bold">Login</CardTitle>
                    <CardDescription>Informe login e senha</CardDescription>
                </CardHeader>
                <CardContent>
                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label htmlFor="email">Email</Label>
                            <Input id="email" type="email" name="email" placeholder="example@example.com" required />
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="password">Password</Label>
                            <Input id="password" type="password" name="password" required />
                        </div>
                        <Button type="submit" className="w-full">
                            Login
                        </Button>

                        {erro && (
                            <Alert variant="destructive" className="text-left">
                                <AlertCircle className="h-4 w-4" />
                                <AlertTitle className="text-left"><b>Erro</b></AlertTitle>
                                <AlertDescription className="text-left">
                                    Email ou senha inválidos. Tente novamente.
                                </AlertDescription>
                            </Alert>
                        )}

                        <div className="mt-4 space-y-2">
                            <a href="#" className="text-sm underline">Esqueci minha senha</a>
                            <p className="text-sm">
                                Não tem uma conta? <a href="#" className="underline">Criar conta</a>
                            </p>
                        </div>
                    </div>
                </CardContent>
            </Card>
        </form>
    );
};

export default Login;

