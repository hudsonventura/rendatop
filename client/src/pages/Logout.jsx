import React, { useEffect, useState } from 'react';
import { TrendingUp, LogOut as LogOutIcon } from 'lucide-react';
import axiosInstance from '@/utils/axiosConfig';
import { appPath } from "@/utils/appPath";

const Logout = () => {
    const [clearing, setClearing] = useState(true);

    useEffect(() => {
        // Tell the server to clear the HttpOnly cookie
        axiosInstance.post('/logout')
            .catch(() => { /* ignore errors — clear locally regardless */ })
            .finally(() => {
                sessionStorage.clear();
                const timer = setTimeout(() => {
                    setClearing(false);
                    window.location.href = appPath('/login');
                }, 800);
            });
    }, []);

    return (
        <div className="min-h-screen bg-background flex items-center justify-center">
            <div className="text-center space-y-4">
                <div className="flex items-center justify-center gap-3 mb-6">
                    <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
                        <TrendingUp className="h-6 w-6 text-primary" />
                    </div>
                </div>
                <div className="flex items-center justify-center gap-2 text-muted-foreground">
                    <LogOutIcon className="h-5 w-5 animate-pulse" />
                    <span className="text-lg">Saindo...</span>
                </div>
                <div className="h-1 w-32 mx-auto bg-muted rounded-full overflow-hidden">
                    <div className="h-full bg-primary rounded-full animate-pulse" style={{ width: clearing ? '80%' : '100%' }} />
                </div>
            </div>
        </div>
    );
};

export default Logout;
