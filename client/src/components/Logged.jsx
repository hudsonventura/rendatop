import React from 'react';
import { useEffect } from 'react';
import axiosInstance from '../utils/axiosConfig';
import { appPath } from "@/utils/appPath";

/**
 * Componente que garante que a página só é acessível se o usuário estiver logado.
 * Redireciona para /login se o cookie JWT não for válido ou estiver ausente.
 */
const Logged = () => {

    useEffect(() => {
        axiosInstance
            .get("/Authenticated")
            .then(() => {
                // Cookie válido — nada a fazer
            })
            .catch((error) => {
                if (error.response?.status === 401) {
                    sessionStorage.clear();
                    window.location.href = appPath('/login');
                }
            });
    }, []);

    return <></>;
};

export default Logged;
