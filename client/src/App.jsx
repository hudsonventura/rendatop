import { useState, useEffect } from 'react'
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'
import { Button } from "@/components/ui/button"

import { Terminal } from "lucide-react"
 
import {
  Alert,
  AlertDescription,
  AlertTitle,
} from "@/components/ui/alert"


import { useToast } from "@/hooks/use-toast";

import axiosInstance from "@/utils/axiosConfig";

import Login from "@/pages/Login"
import Home from "@/pages/Home"
import Header from "@/components/Header"
import Logout from "@/pages/Logout"
import Logged from "@/components/Logged"


function App() {
  const [count, setCount] = useState(0)
  const { toast } = useToast()

  


  return (
    <Router>
        <div>
            <nav>
                
            </nav>
            <Routes>
            <Route path="/" element={<Login />} />
            <Route path="/login" element={<Login />} />
            <Route path="/logout" element={<Logout />} />
                <Route path="/home" 
                    element={
                        <>
                            <Logged />
                            <Header />
                            <Home />
                        </>
                    } 
                />
            </Routes>
        </div>
    </Router>
    );
  
}

export default App
