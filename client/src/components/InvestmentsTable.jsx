import { useEffect, useState } from "react";
import axiosInstance from "../utils/axiosConfig";

import InvestmentsContent from "@/components/InvestmentsContent"
import InvestmentsTitle from "@/components/InvestmentsTitle"

import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from "@/components/ui/accordion"





export default function InvestmentsTable({investments}) {

	return (
		<>
			{investments.map((investment, index) => (
				<Accordion key={investment.id} type="single" collapsible className="w-full" style={{ width: "80rem" }}>
					<AccordionItem value={`item-${index}`}>
						<AccordionTrigger className="w-full bg-white text-black"><InvestmentsTitle investment={investment} /></AccordionTrigger>
						<AccordionContent>
							<InvestmentsContent investment={investment} />
						</AccordionContent>
					</AccordionItem>
				</Accordion>
			))}
		</>
	);
}
