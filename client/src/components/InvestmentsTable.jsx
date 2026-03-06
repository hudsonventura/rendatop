import { useEffect, useState } from "react";
import axiosInstance from "@/utils/axiosConfig";

import InvestmentsContent from "@/components/InvestmentsContent"
import InvestmentsTitle from "@/components/InvestmentsTitle"

import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from "@/components/ui/accordion"


export default function InvestmentsTable({ investments, setReload }) {

	return (
		<div className="space-y-2">
			{investments.map((investment, index) => (
				<Accordion key={investment.id} type="single" collapsible className="w-full">
					<AccordionItem value={`item-${index}`} className="border rounded-lg overflow-hidden">
						<AccordionTrigger className="w-full px-4 py-3 hover:bg-accent/50 transition-colors [&[data-state=open]]:bg-accent/30">
							<InvestmentsTitle investment={investment} />
						</AccordionTrigger>
						<AccordionContent className="border-t">
							<InvestmentsContent investment={investment} setReload={setReload} />
						</AccordionContent>
					</AccordionItem>
				</Accordion>
			))}
		</div>
	);
}
