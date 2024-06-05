<component>
	<property name="Wins" type="int"/>
	<property name="GoodSkips" type="int"/>
	<property name="BadSkips" type="int"/>
	<property name="CanGoldSkip" type="bool"/>
	<property name="CanSkip" type="bool"/>
	<property name="CanQuit" type="bool"/>
	<property name="CanForceGoldSkip" type="bool"/>
	<property name="CanForceSkip" type="bool"/>
	<property name="CanForceQuit" type="bool"/>
	<property name="PublishDate" type="string"/>
	<property name="WinMedal" type="string"/>
	<property name="GoodSkipMedal" type="string"/>
	<property name="GoodSkipMedalName" type="string"/>
	<property type="string" name="id" default="rmt-widget" />
	
	<template>
		<frame pos="-155 85" size="62 31" id="{{id}}">
			<quad pos="0 0" size="62 31" z-index="0" bgcolor="10101070"/>
			<quad pos="1 -1" size="10 10" z-index="1" style="MedalsBig" substyle="{{WinMedal}}" autoscale="1" keepratio="Fit"/>
			<label pos="21 -3.2" z-index="1" text="{{Wins}}" textsize="3.5" textfont="RajdhaniMono" halign="center" />
			<quad pos="32 -1" size="10 10" z-index="1" style="MedalsBig" substyle="{{GoodSkipMedal}}" autoscale="1" keepratio="Fit"/>
			<label pos="52 -3.2" z-index="1" text="{{GoodSkips}}" textsize="3.5" textfont="RajdhaniMono" halign="center" />
			<label if="CanGoldSkip" pos="15 -18" z-index="1" text="Vote {{GoodSkipMedalName}} Skip" textsize="2.5" textfont="RajdhaniMono" halign="center" style="CardButtonSmallS" scriptevents="1" action="VoteGoldSkip"/>
			<label if="CanSkip" pos="15 -18" z-index="1" text="Vote Skip" textsize="2.5" textfont="RajdhaniMono" halign="center" style="CardButtonSmallS" scriptevents="1" action="VoteSkip" />
			<label if="CanQuit" pos="15 -12" z-index="1" text="Vote Quit" textsize="2.5" textfont="RajdhaniMono" halign="center" style="CardButtonSmallS" scriptevents="1" action="VoteQuit" />
			<label if="CanForceGoldSkip" pos="45 -18" z-index="1" text="Force {{GoodSkipMedalName}} Skip" textsize="2.5" textfont="RajdhaniMono" halign="center" style="CardButtonSmallS" scriptevents="1" action="ForceGoldSkip" bgcolor="00A565FF"/>
			<label if="CanForceSkip" pos="45 -18" z-index="1" text="Force Skip" textsize="2.5" textfont="RajdhaniMono" halign="center" style="CardButtonSmallS" scriptevents="1" action="ForceSkip" bgcolor="00A565FF"/>
			<label if="CanForceQuit" pos="45 -12" z-index="1" text="Force Quit" textsize="2.5" textfont="RajdhaniMono" halign="center" style="CardButtonSmallS" scriptevents="1" action="ForceQuit" bgcolor="00A565FF"/>
			<label pos="45 -30" z-index="1" text="Skips: {{BadSkips}}" textsize="1.5" textfont="RajdhaniMono" halign="left" valign="bottom"/>
			<label pos="2 -30" z-index="1" text="Published: {{PublishDate}}" textsize="1.5" textfont="RajdhaniMono" halign="left" valign="bottom"/>
		</frame>
	</template>


	<!--<script resource="TrackmaniaRandomMapServer.Manialinks.Scripts.Draggable.ms" />-->
</component>