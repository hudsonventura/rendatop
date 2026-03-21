import React from 'react';
import { Button } from "@/components/ui/button";
import { Home } from "lucide-react";
import { useNavigate } from "react-router-dom";

const NotFound = () => {
    const navigate = useNavigate();

    return (
        <div className="min-h-screen flex items-center justify-center bg-background p-6">
            <div className="text-center space-y-6 max-w-md">
                <div className="flex justify-center mb-8">
                    <div className="relative">
                        <div className="absolute inset-0 bg-primary/20 blur-xl rounded-full" />
                        <img src="/favicon.svg" alt="RendaTop" className="h-32 w-32 relative z-10" />
                    </div>
                </div>
                
                <h1 className="text-4xl font-extrabold tracking-tight lg:text-5xl text-foreground">
                    404
                </h1>
                
                <p className="text-xl text-muted-foreground font-medium">
                    Oops! Página não encontrada
                </p>
                
                <p className="text-muted-foreground">
                    A página que você está procurando pode ter sido removida, teve seu nome alterado ou está temporariamente indisponível.
                </p>

                <div className="pt-6">
                    <Button 
                        size="lg" 
                        className="w-full sm:w-auto flex items-center gap-2 m-auto"
                        onClick={() => navigate('/')}
                    >
                        <Home className="h-4 w-4" />
                        Voltar para o Início
                    </Button>
                </div>
            </div>
        </div>
    );
};

export default NotFound;
