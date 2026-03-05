import React from 'react';
import { useState, useEffect } from "react";
import axiosInstance from "../utils/axiosConfig";


import InvestmentsTable from "../components/InvestmentsTable";
import InvestmentsAdd from "../components/InvestmentsAdd";
import InvestmentsResume from "@/components/InvestmentsResume"

const Home = () => {

    const [investments, setInvestments] = useState([]);
    const [reload, setReload] = useState(0);
    useEffect(() => {
        axiosInstance
            .get("/Investments")
            .then((response) => {
                setInvestments(response.data);
            })
            .catch((err) => {
                console.error("Erro ao buscar investimentos:", err);
            });
    }, [reload]);


    return (
        <>
            <h1>Home Page</h1>
            <InvestmentsResume investments={investments} />
            <InvestmentsAdd setReload={setReload} />
            <InvestmentsTable investments={investments} />
        </>
    );
};

export default Home;
