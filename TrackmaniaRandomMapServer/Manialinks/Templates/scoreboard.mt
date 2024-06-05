<component>
	<using namespace="System.Linq"/>
	
	<property name="Wins" type="int"/>
	<property name="GoodSkips" type="int"/>
	<property name="BadSkips" type="int"/>
	<property name="TimeLeft" type="string"/>
	<property name="WinMedal" type="string"/>
	<property name="GoodSkipMedal" type="string"/>
	<property name="WinColor" type="string"/>
	<property name="GoodSkipColor" type="string"/>	
	<property name="Players" type="TrackmaniaRandomMapServer.Models.LeaderboardItem[]"/>

	<template>
		<frame pos="-80 80" size="160 160" z-idex="3">
			<quad pos="0 0" size="160 160"  bgcolor="101010EE"/>
			<label pos="5 -155" size="155 5" textsize="5" text="Skips: {{BadSkips}}" textfont="RajdhaniMono" valign="center" halign="left"/>
			<frame pos="20 -5" size="120 50" z-idex="4">
				<label pos="60 -5" size="60 10" text="RMT SCORE" textsize="10"
					   halign="center" valign="center" style="ManiaPlanetLogos" />
				<quad pos="5 -15"   size="20 20" style="MedalsBig" substyle="{{WinMedal}}" autoscale="1" keepratio="Fit"/>
				<label pos="40 -25" size="30 20"  text="{{Wins}}" textsize="8" textfont="RajdhaniMono" valign="center" halign="right" />
				<quad pos="70 -15"  size="20 20" style="MedalsBig" substyle="{{GoodSkipMedal}}" autoscale="1" keepratio="Fit"/>
				<label pos="100 -25" size="30 20" text="{{GoodSkips}}" textsize="8" textfont="RajdhaniMono" valign="center" halign="right" />
				<label pos="5 -38" text="TIME LEFT: {{TimeLeft}}" textsize="5" textfont="RajdhaniMono" />
			</frame>
			<frame pos="20 -50" size="120 100" z-idex="4">
				<frame foreach="int i in Enumerable.Range(0, Players.Length)" pos="0 {{ -10 * i }}" size="120 10">
					<quad pos="0 0" size="120 10" bgcolor="eeeeeeFF"/>
					<quad pos="1 -0.5" size="90 9" bgcolor="513877FF"/>
					<quad pos="90 -0.5" size="15 9" bgcolor="{{WinColor}}FF"/>
					<quad pos="105 -0.5" size="14.5 9" bgcolor="{{GoodSkipColor}}FF"/>
					<quad pos="90 0" size="0.5 10" bgcolor="eeeeeeFF"/>
					<quad pos="105 0" size="0.5 10" bgcolor="eeeeeeFF"/>
					<label pos="3 -4.5" size="55 9" text="{{Players[i].DisplayName}}" textsize="3.5" textfont="RajdhaniMono"
									   halign="left" valign="center" />
					<label pos="88 -4.5" size="55 9" text="{{Players[i].BestTime}}" textsize="3.5" textfont="RajdhaniMono"
									   halign="right" valign="center" />
					<label pos="97.5 -4.5" text="{{Players[i].NumWins}}" textsize="3.5" textfont="RajdhaniMono"
									   halign="center" valign="center"/>
					<label pos="112.5 -4.5" text="{{Players[i].GoodSkips}}" textsize="3.5" textfont="RajdhaniMono"
									   halign="center" valign="center" />
				</frame>
			</frame>
		</frame>
	</template>
</component>